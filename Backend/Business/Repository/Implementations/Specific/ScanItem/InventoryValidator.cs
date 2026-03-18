using Business.Repository.Interfaces.Specific.ParametersModule;
using Business.Repository.Interfaces.Specific.System;
using Business.Services.CacheItem;
using Entity.DTOs.ScanItem;
using Entity.Models.ScanItems;
using Entity.Models.System;
using Utilities.Enums.Models;

namespace Business.Repository.Implementations.Specific.ScanItem
{
    /// <summary>
    /// Implementación de la lógica de validación específica para las operaciones de Inventario (inicio, escaneo, finalización y verificación).
    /// </summary>
    public class InventoryValidator : IInventoryValidator
    {
        private readonly IStateItemBusiness _stateItemBusiness;
        private readonly ICategoryBusiness _categoryBusiness;
        private readonly IItemBusiness _itemBusiness;

        public InventoryValidator(IStateItemBusiness stateItemBusiness, ICategoryBusiness categoryBusiness, IItemBusiness itemBusiness)
        {
            _stateItemBusiness = stateItemBusiness;
            _categoryBusiness = categoryBusiness;
            _itemBusiness = itemBusiness;
        }

        // --- Escaneo ---
        public void EnsureInventoryInProgress(Inventary? inventary)
        {
            if (inventary == null || inventary.Zone.StateZone != StateZone.InInventory)
                throw new InvalidOperationException("El inventario no está en progreso.");
        }

        public void EnsureItemExists(Item? item)
        {
            if (item == null)
                throw new InvalidOperationException("El ítem no existe en el sistema.");
        }

        public void EnsureNotDuplicate(int inventaryId, int itemId, IInventoryCacheService cache)
        {
            if (cache.Exists(inventaryId, itemId))
                throw new InvalidOperationException("Este ítem ya fue escaneado.");
        }

        public string ValidateItemZone(Item item, Zone zone)
            => item.ZoneId == zone.Id ? "Correct" : "WrongZone";

        // --- Inicio ---
        public void EnsureZoneAvailable(Zone? zone)
        {
            if (zone == null)
                throw new InvalidOperationException("La zona no existe.");
            if (zone.StateZone != StateZone.Available)
                throw new InvalidOperationException("La zona no está disponible para inventario.");
        }

        // --- Finalización ---
        public void EnsureZoneInInventory(Zone? zone)
        {
            if (zone == null || zone.StateZone != StateZone.InInventory)
                throw new InvalidOperationException("La zona no está en estado de inventario.");
        }

        // --- Verificación ---
        public void EnsurePendingVerification(Inventary? inventary)
        {
            if (inventary == null || inventary.Zone.StateZone != StateZone.InVerification)
                throw new InvalidOperationException("El inventario no está en verificación.");
        }

        public void EnsureNotAlreadyVerified(Inventary inventary)
        {
            if (inventary.Verification != null)
                throw new InvalidOperationException("El inventario ya fue verificado.");
        }

        public void EnsureSameBranch(Checker checker, Inventary inventary)
        {
            if (checker.BranchId != inventary.Zone.BranchId)
                throw new InvalidOperationException("El checker no pertenece a la misma sucursal que el inventario.");
        }

        public async Task<VerificationComparisonDto> CompareCacheWithInventary(Inventary inventary, IEnumerable<ScannedItem> scans)
        {
            var report = new VerificationComparisonDto
            {
                InventaryId = inventary.Id
            };

            // Mapeos rápidos
            var itemsInZone = inventary.Zone.Items.ToDictionary(i => i.Id, i => i);
            var scannedItems = scans.ToDictionary(s => s.ItemId, s => s);

            // Cargar estados y categorías una sola vez (OPTIMIZADO)
            var allStates = (await _stateItemBusiness.GetAllAsync())
                .ToDictionary(s => s.Id, s => s.Name);

            var allCategories = (await _categoryBusiness.GetAllAsync())
                .ToDictionary(c => c.Id, c => c.Name);

            // Helpers
            string GetStateName(int stateId) =>
                allStates.TryGetValue(stateId, out var name) ? name : "Desconocido";

            string GetCategoryName(int? id) =>
                id.HasValue && allCategories.TryGetValue(id.Value, out var name)
                    ? name
                    : "Sin categoría";

            // -----------------------------------------
            // 1. MISSING ITEMS
            // -----------------------------------------
            foreach (var expected in itemsInZone.Values)
            {
                if (!scannedItems.ContainsKey(expected.Id))
                {
                    report.MissingItems.Add(new MissingItemDto
                    {
                        ItemId = expected.Id,
                        Code = expected.Code!,
                        Name = expected.Name!,
                        ExpectedState = expected.StateItem?.Name ?? "Desconocido",
                        ScannedStateName = "Perdido",
                        CategoryName = GetCategoryName(expected.CategoryItemId),
                        Reason = "No escaneado",
                        SuggestedAction = "Revisar en la zona o con responsable"
                    });
                }
            }

            // -----------------------------------------
            // 2. UNEXPECTED ITEMS
            // -----------------------------------------
            foreach (var scan in scans)
            {
                if (!itemsInZone.ContainsKey(scan.ItemId))
                {
                    var fullItem = await _itemBusiness.GetByIdAsync(scan.ItemId);

                    report.UnexpectedItems.Add(new UnexpectedItemDto
                    {
                        ItemId = scan.ItemId,
                        Code = fullItem?.Code ?? "SIN_CODIGO",
                        Name = fullItem?.Name ?? "SIN_NOMBRE",
                        ZoneOrigen = fullItem?.ZoneName ?? "Zona desconocida",
                        Reason = "No pertenece a esta zona",
                        SuggestedAction = "Mover a la zona correcta o poner en cuarentena"
                    });
                }
            }

            // -----------------------------------------
            // 3. STATE MISMATCHES (con regla PERDIDO → Missing)
            // -----------------------------------------
            foreach (var scan in scans)
            {
                if (!itemsInZone.TryGetValue(scan.ItemId, out var expected))
                    continue;

                if (scan.StateItemId == expected.StateItemId)
                    continue;

                var scannedStateName = GetStateName(scan.StateItemId);
                var categoryName = GetCategoryName(expected.CategoryItemId);

                if (scannedStateName.Equals("Perdido", StringComparison.OrdinalIgnoreCase))
                {
                    report.MissingItems.Add(new MissingItemDto
                    {
                        ItemId = expected.Id,
                        Code = expected.Code!,
                        Name = expected.Name!,
                        ExpectedState = expected.StateItem?.Name ?? "Desconocido",
                        ScannedStateName = "Perdido",
                        CategoryName = categoryName,
                        Reason = "Marcado como perdido",
                        SuggestedAction = "Revisar en la zona o con responsable"
                    });

                    continue;
                }

                report.StateMismatches.Add(new StateMismatchDto
                {
                    ItemId = expected.Id,
                    Code = expected.Code!,
                    Name = expected.Name!,
                    ExpectedState = expected.StateItem?.Name ?? "Desconocido",
                    ScannedState = scan.StateItemId.ToString(),
                    ScannedStateName = scannedStateName,
                    CategoryName = categoryName,
                    Reason = "Cambio de estado",
                    SuggestedAction = "Revisar físicamente o actualizar registro"
                });
            }

            // -----------------------------------------
            // 4. Resumen
            // -----------------------------------------
            report.ShortSummary =
                $"Se detectaron {report.MissingItems.Count} faltantes, " +
                $"{report.UnexpectedItems.Count} inesperados y " +
                $"{report.StateMismatches.Count} discrepancias de estado.";

            return report;
        }

    }
}
