using Business.Abstractions;
using Business.Helper;
using Business.Repository.Interfaces.Specific.ScanItem;
using Business.Services.InventaryItem;
using Entity.DTOs.ScanItem;
using Entity.DTOs.System.Verification;
using Utilities.Enums.Models;

namespace Business.Repository.Implementations.Specific.ScanItem
{
    /// <summary>
    /// Servicio de negocio responsable de finalizar un proceso de inventario iniciado.
    /// </summary>
    public class InventoryFinishService : IInventoryFinishService
    {
        private readonly IInventoryRepository _repository;
        private readonly IInventoryValidator _validator;
        private readonly IRealtimeUpdateService _realtimeUpdateService;

        public InventoryFinishService(IInventoryRepository repository, IInventoryValidator validator, IRealtimeUpdateService realtimeUpdateService)
        {
            _repository = repository;
            _validator = validator;
            _realtimeUpdateService = realtimeUpdateService;
        }

        /// <summary>
        /// Finaliza el proceso de inventario actual, actualizando su estado a "En Verificación" y guardando las observaciones finales.
        /// </summary>
        public async Task<FinishInventoryResponseDto> FinishAsync(FinishInventoryRequestDto request)
        {
            var inventary = await _repository.GetInventaryWithZoneAsync(request.InventaryId);

            _validator.EnsureInventoryInProgress(inventary);

            inventary!.Observations += $" | Cierre: {request.Observations}";
            inventary.Zone.StateZone = StateZone.InVerification;
            inventary.Status = InventaryStatus.Verified;
            inventary.InvitationCode = null;

            await _repository.SaveChangesAsync();

            var payload = ZoneStateMapper.Map(inventary.Zone);
            await _realtimeUpdateService.SendUpdateToAllAsync("ReceiveZoneStateUpdate", payload);


            var listPayload = new VerificationListUpdateDTO
            {
                InventaryId = inventary.Id,
                Date = inventary.Date,
                ZoneId = inventary.ZoneId,
                ZoneName = inventary.Zone.Name,
                BranchId = inventary.Zone.BranchId,
                UpdateType = "Added"
            };
            await _realtimeUpdateService.SendUpdateToAllAsync("ReceiveVerificationListUpdate", listPayload);
            // -----------------------------------------------------------

            return new FinishInventoryResponseDto
            {
                InventaryId = inventary.Id,
                StateZone = inventary.Zone.StateZone.ToString(),
                Observations = inventary.Observations
            };
        }
    }
}
