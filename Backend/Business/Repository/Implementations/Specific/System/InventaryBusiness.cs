using AutoMapper;
using Business.Abstractions;
using Business.Helper;
using Business.Repository.Interfaces.Specific.System;
using Business.Services.CacheItem;
using Data.Factory;
using Data.Repository.Interfaces.General;
using Data.Repository.Interfaces.Specific.System;
using Data.Repository.Interfaces.Strategy.Delete;
using Entity.DTOs.System.Inventary;
using Entity.DTOs.System.Inventary.AreaManager.InventoryDetail;
using Entity.DTOs.System.Inventary.AreaManager.InventorySummary;
using Entity.Models.System;
using Microsoft.Extensions.Logging;

namespace Business.Repository.Implementations.Specific.System
{
    /// <summary>
    /// Implementación de la lógica de negocio para la gestión de Inventarios (Inventary) o procesos de inventario.
    /// </summary>
    public class InventaryBusiness :
        GenericBusinessDualDTO<Inventary, InventaryConsultDTO, InventaryDTO>,
        IInventaryBusiness
    {

        private readonly IGeneral<Inventary> _general;
        private readonly IInventary _inventary;
        private readonly IInventoryCacheService _cacheService;
        private readonly IRealtimeUpdateService _realtimeUpdateService;

        public InventaryBusiness(
            IDataFactoryGlobal factory,
            IGeneral<Inventary> general,
            IInventary inventary,
            IInventoryCacheService cacheService,
            IRealtimeUpdateService realtimeUpdateService,
            IDeleteStrategyResolver<Inventary> deleteStrategyResolver,
            ILogger<Inventary> logger,
            IMapper mapper)
            : base(factory.CreateInventaryData(), deleteStrategyResolver, logger, mapper)
        {
            _general = general;
            _inventary = inventary;
            _cacheService = cacheService;
            _realtimeUpdateService = realtimeUpdateService;
        }

        // General 

        /// <summary>
        /// Obtiene todos los registros de inventario, incluyendo los inactivos.
        /// </summary>
        public async Task<IEnumerable<InventaryConsultDTO>> GetAllTotalAsync()
        {
            var active = await _general.GetAllTotalAsync();
            return _mapper.Map<IEnumerable<InventaryConsultDTO>>(active);
        }


        // Specific

        /// <summary>
        /// Obtiene el historial de inventario para un grupo operativo (OperationalGroup) específico.
        /// </summary>
        public async Task<IEnumerable<InventoryHistoryDTO>> GetInventoryHistoryAsync(int groupId)
        {
            var entities = await _inventary.GetInventoryHistoryByGroupAsync(groupId);
            return _mapper.Map<IEnumerable<InventoryHistoryDTO>>(entities);
        }

        /// <summary>
        /// Obtiene un resumen consolidado del inventario para una zona específica.
        /// </summary>
        public async Task<InventorySummaryResponseDTO> GetInventorySummaryAsync(int zoneId)
        {
            var summary = await _inventary.GetInventorySummaryAsync(zoneId);
            return _mapper.Map<InventorySummaryResponseDTO>(summary);
        }

        /// <summary>
        /// Obtiene el detalle completo de un registro de inventario específico.
        /// </summary>
        public async Task<InventoryDetailResponseDTO> GetInventoryDetailAsync(int inventoryId)
        {
            var detail = await _inventary.GetInventoryDetailAsync(inventoryId);
            return _mapper.Map<InventoryDetailResponseDTO>(detail);
        }

        /// <summary>
        /// Cancela un inventario activo: elimina el registro de BD y limpia el caché de escaneos
        /// </summary>
        public async Task<bool> CancelInventoryAsync(int inventoryId)
        {
            try
            {
                _cacheService.ClearScans(inventoryId);

                var affectedZone = await _inventary.CancelInventoryAsync(inventoryId);

                if (affectedZone != null)
                {
                    var payload = ZoneStateMapper.Map(affectedZone);
                    await _realtimeUpdateService.SendUpdateToAllAsync("ReceiveZoneStateUpdate", payload);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en CancelInventoryAsync para inventoryId: {inventoryId}");
                throw;
            }
        }
    }
}
