using Entity.DTOs.System.Inventary.AreaManager.InventoryDetail;
using Entity.DTOs.System.Inventary.AreaManager.InventorySummary;
using Entity.Models.System;


namespace Data.Repository.Interfaces.Specific.System
{
    /// <summary>
    /// Repositorio para inventarios
    /// </summary>
    public interface IInventary : IGenericData<Inventary>
    {
        /// <summary>
        /// Agrega un nuevo inventario sin retornar la entidad
        /// </summary>
        Task AddAsync(Inventary entity);

        /// <summary>
        /// Obtiene historial de inventarios de un grupo operativo
        /// </summary>
        Task<IEnumerable<Inventary>> GetInventoryHistoryByGroupAsync(int groupId);

        /// <summary>
        /// Obtiene resumen de inventarios de una zona
        /// </summary>
        Task<InventorySummaryResponseDTO> GetInventorySummaryAsync(int zoneId);

        /// <summary>
        /// Obtiene detalle completo de un inventario
        /// </summary>
        Task<InventoryDetailResponseDTO?> GetInventoryDetailAsync(int inventoryId);

        /// <summary>
        /// Obtiene inventario por código de invitación
        /// </summary>
        Task<Inventary?> GetByInvitationCodeAsync(string code);

        /// <summary>
        /// Verifica si un código de invitación ya existe
        Task<bool> CheckIfInvitationCodeExistsAsync(string code);

        /// <summary>
        /// Cancela un inventario (elimina registro y limpia caché)
        /// </summary>
        Task<Zone?> CancelInventoryAsync(int inventoryId);
    }
}
