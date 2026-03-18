using System.ComponentModel.DataAnnotations;

namespace Entity.DTOs.ParametersModels.Notification.RQS
{
    /// <summary>
    /// DTO para la solicitud de creación de una notificación de inventario.
    /// </summary>
    public class CreateInventoryRequestRQS
    {
        public int UserId { get; set; }
        public InventoryRequestContentDTO Content { get; set; } = new();
    }
}
