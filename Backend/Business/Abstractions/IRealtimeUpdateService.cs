namespace Business.Abstractions
{
    /// <summary>
    /// Abstracción genérica para enviar actualizaciones en tiempo real a los clientes.
    /// Sigue el Principio de Inversión de Dependencias (DIP).
    /// La capa de Negocio depende de esta interfaz, no de SignalR directamente.
    /// </summary>
    public interface IRealtimeUpdateService
    {
        /// <summary>
        /// Envía un mensaje a un grupo de usuarios específico (basado en su ID de usuario).
        /// </summary>
        /// <typeparam name="T">El tipo de dato del payload.</typeparam>
        /// <param name="userId">El ID del usuario (se usará para formar el nombre del grupo, ej: "User-123").</param>
        /// <param name="topic">El "tema" o método que el cliente escuchará (ej: "ReceiveNotification").</param>
        /// <param name="payload">Los datos a enviar.</param>
        Task SendUpdateToUserAsync<T>(string userId, string topic, T payload);

        /// <summary>
        /// Envía un mensaje a todos los clientes conectados.
        /// </summary>
        Task SendUpdateToAllAsync<T>(string topic, T payload);

        /// <summary>
        /// Envía un mensaje a un grupo de SignalR específico (ej: "Inventary-101").
        /// </summary>
        Task SendUpdateToGroupAsync<T>(string groupName, string topic, T payload);
    }
}