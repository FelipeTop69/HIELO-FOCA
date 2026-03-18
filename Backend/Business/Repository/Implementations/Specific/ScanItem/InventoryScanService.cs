using Business.Abstractions;
using Business.Repository.Interfaces.Specific.ScanItem;
using Business.Services.CacheItem;
using Business.Services.InventaryItem;
using Entity.DTOs.ScanItem;
using Entity.DTOs.ScanItem.Missing;
using Entity.Models.ScanItems;
using Entity.Models.System;

namespace Business.Repository.Implementations.Specific.ScanItem
{
    /// <summary>
    /// Servicio de negocio responsable de procesar el escaneo individual de ítems durante un inventario.
    /// </summary>
    public class InventoryScanService : IInventoryScanService
    {
        private readonly IInventoryRepository _repository;
        private readonly IInventoryCacheService _cache;
        private readonly IInventoryValidator _validator;
        private readonly IRealtimeUpdateService _realtimeUpdateService;

        public InventoryScanService(
            IInventoryRepository repository,
            IInventoryCacheService cache,
            IInventoryValidator validator,
            IRealtimeUpdateService realtimeUpdateService)
        {
            _repository = repository;
            _cache = cache;
            _validator = validator;
            _realtimeUpdateService = realtimeUpdateService;
        }

        /// <summary>
        /// Registra el escaneo de un ítem. Valida su existencia, duplicidad y pertenencia a la zona del inventario actual.
        /// </summary>
        public async Task<ScanResponseDto> ScanAsync(ScanRequestDto request, int userId)
        {
            var inventary = await _repository.GetInventaryWithZoneAsync(request.InventaryId);
            _validator.EnsureInventoryInProgress(inventary);

            var item = await _repository.GetItemByCodeAsync(request.Code);
            _validator.EnsureItemExists(item);

            _validator.EnsureNotDuplicate(request.InventaryId, item!.Id, _cache);

            var status = _validator.ValidateItemZone(item, inventary!.Zone);

            _cache.AddScan(request.InventaryId, new ScannedItem
            {
                ItemId = item.Id,
                Status = status,
                ScanTime = DateTime.Now, 
                StateItemId = request.StateItemId
            });


            if (status == "Correct")
            {
                var payload = new
                {
                    ItemId = item.Id,
                    StateItemId = request.StateItemId,
                    InventaryId = request.InventaryId
                };

                // Definir el nombre del grupo
                string groupName = $"Inventary-{request.InventaryId}";

                // Enviar la actualización a TODO el grupo
                await _realtimeUpdateService.SendUpdateToGroupAsync(groupName, "ReceiveItemUpdate", payload);
            }

            // Retornar la respuesta HTTP normal al movil
            return new ScanResponseDto
            {
                IsValid = status == "Correct",
                Status = status,
                Message = status == "Correct" ? "Item validado correctamente." : "Item pertenece a otra zona.",
                ItemId = item.Id
            };
        }

        /// <summary>
        /// Consulta los ítems faltantes en un inventario basado en los escaneos realizados.
        /// </summary>
        public async Task<List<MissingItemDTO>> GetMissingItemsAsync(int inventaryId)
        {
            // Obtener el inventario y TODOS los ítems que deberían estar en esa zona
            var inventary = await _repository.GetInventaryWithZoneAsync(inventaryId);

            if (inventary == null)
                throw new KeyNotFoundException("El inventario no existe.");

            // Obtener lo que ya se ha escaneado 
            var scannedItems = _cache.GetScans(inventaryId);

            // Crear un HashSet de IDs escaneados para búsqueda rápida 
            var scannedItemIds = scannedItems.Select(s => s.ItemId).ToHashSet();

            // Filtrar: Ítems de la Zona que NO están en el HashSet de escaneados
            var missingItems = inventary.Zone.Items
                .Where(item => !scannedItemIds.Contains(item.Id))
                .Select(item => new MissingItemDTO
                {
                    ItemId = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    CurrentState = item.StateItem?.Name ?? "Desconocido"
                })
                .ToList();

            return missingItems;
        }

        public async Task RegisterManualScansAsync(ManualScanRequestDTO request)
        {
            // Validar que el inventario siga activo
            var inventary = await _repository.GetInventaryWithZoneAsync(request.InventaryId);
            _validator.EnsureInventoryInProgress(inventary);

            // Procesar cada ítem manual
            foreach (var entry in request.Items)
            {
                // Buscar la info real del ítem en la lista cargada de la zona
                var itemRef = inventary!.Zone.Items.FirstOrDefault(i => i.Id == entry.ItemId);

                if (itemRef != null)
                {
                    string stateName = GetStateNameById(entry.StateItemId);

                    // Construimos el objeto de caché
                    var manualScan = new ScannedItem
                    {
                        ItemId = itemRef.Id,
                        Code = itemRef.Code,
                        Name = itemRef.Name,
                        ScanTime = DateTime.Now,
                        Status = "Correct",
                        StateItemId = entry.StateItemId,
                        StateItemName = stateName
                    };

                    // 4. Agregamos al caché
                    _cache.AddScan(request.InventaryId, manualScan);
                }
            }
        }

        private string GetStateNameById(int id)
        {
            return id switch
            {
                1 => "En orden",
                2 => "Reparación",
                3 => "Dañado",
                4 => "Perdido",
                _ => "Desconocido"
            };
        }

        public async Task<ItemScanStatusDto> CheckIfScannedAsync(int inventaryId, string code)
        {
            var item = await _repository.GetItemByCodeAsync(code);

            if (item == null)
            {
                return new ItemScanStatusDto
                {
                    IsScanned = false,
                    ItemId = null,
                    Message = "El ítem no existe."
                };
            }

            bool exists = _cache.Exists(inventaryId, item.Id);

            return new ItemScanStatusDto
            {
                IsScanned = exists,
                ItemId = item.Id,
                Message = exists ? "El ítem ya fue escaneado." : null
            };
        }

    }
}
