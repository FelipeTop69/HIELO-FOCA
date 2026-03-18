using Business.Abstractions;
using Business.Helper;
using Business.Repository.Interfaces.Specific.ScanItem;
using Business.Services.CacheItem;
using Business.Services.InventaryItem;
using Data.Repository.Interfaces.Specific.System;
using Entity.DTOs.ScanItem;
using Entity.Models.System;
using Utilities.Enums.Models;
using Utilities.Helpers;

namespace Business.Repository.Implementations.Specific.ScanItem
{
    /// <summary>
    /// Servicio de negocio responsable de iniciar un nuevo proceso de inventario en una zona específica.
    /// </summary>
    public class InventoryStartService : IInventoryStartService
    {
        private readonly IInventary _data;
        private readonly IInventoryRepository _repository;
        private readonly IInventoryCacheService _cache;
        private readonly IInventoryValidator _validator;
        private readonly IRealtimeUpdateService _realtimeUpdateService;

        public InventoryStartService(
            IInventary data,
            IInventoryRepository repository,
            IInventoryCacheService cache,
            IInventoryValidator validator,
            IRealtimeUpdateService realtimeUpdateService)
        {
            _data = data;
            _repository = repository;
            _cache = cache;
            _validator = validator;
            _realtimeUpdateService = realtimeUpdateService;
        }

        /// <summary>
        /// Crea un nuevo registro de inventario, valida que la zona esté disponible,
        /// genera un código de invitación único, y actualiza el estado de la zona.
        /// </summary>
        public async Task<StartInventoryResponseDto> StartAsync(StartInventoryRequestDto request, int userId)
        {
            var zone = await _repository.GetZoneAsync(request.ZoneId);
            _validator.EnsureZoneAvailable(zone);


            var inventary = new Inventary
            {
                Date = DateTime.Now,
                ZoneId = request.ZoneId,
                OperatingGroupId = request.OperatingGroupId,
                Observations = request.Observations ?? string.Empty,
                Active = true,
                Status = InventaryStatus.InProgress
            };

            string newCode;
            bool codeExists;
            int maxAttempts = 10;
            int attempts = 0;

            do
            {
                if (attempts >= maxAttempts)
                    throw new ApplicationException("No se pudo generar un código de invitación único tras 10 intentos.");

                newCode = InvitationCodeGenerator.Generate(4); // Genera "D8K4"

                codeExists = await _data.CheckIfInvitationCodeExistsAsync(newCode);
                attempts++;

            } while (codeExists);

            inventary.InvitationCode = newCode;

            await _data.AddAsync(inventary);

            zone!.StateZone = StateZone.InInventory;

            await _repository.SaveChangesAsync();

            var payload = ZoneStateMapper.Map(zone);
            await _realtimeUpdateService.SendUpdateToAllAsync("ReceiveZoneStateUpdate", payload);

            _cache.ClearScans(inventary.Id);

            return new StartInventoryResponseDto
            {
                InventaryId = inventary.Id,
                StateZone = zone.StateZone.ToString(),
                StartDate = inventary.Date,
                InvitationCode = inventary.InvitationCode
            };
        }
    }
}
