using Entity.DTOs.ScanItem;
using Entity.DTOs.ScanItem.Missing;
using Entity.Models.System;

namespace Business.Repository.Interfaces.Specific.ScanItem
{
    /// <summary>
    /// Servicio de negocio para el procesamiento y validación de los escaneos de ítems durante el inventario.
    /// </summary>
    public interface IInventoryScanService
    {
        /// <summary>
        /// Procesa el escaneo de un ítem individual, aplicando validaciones de estado y duplicidad.
        /// </summary>
        /// <param name="request">DTO con la información del ítem escaneado (ej. código QR, estado).</param>
        Task<ScanResponseDto> ScanAsync(ScanRequestDto request, int userId);

        /// <summary>
        /// Compara los ítems de la zona contra el caché de escaneos y devuelve los faltantes.
        /// </summary>
        Task<List<MissingItemDTO>> GetMissingItemsAsync(int inventaryId);

        /// <summary>
        /// Registra masivamente ítems en el caché como si hubieran sido escaneados.
        /// </summary>
        Task RegisterManualScansAsync(ManualScanRequestDTO request);
        Task<ItemScanStatusDto> CheckIfScannedAsync(int inventaryId, string code);

    }
}