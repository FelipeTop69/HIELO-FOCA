using Business.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;

namespace Web.Implementations
{
    /// <summary>
    /// Implementación concreta de IRealtimeUpdateService usando SignalR.
    /// Esta clase vive en la capa Web y hace de puente entre Negocio y SignalR.
    /// </summary>
    public class SignalrRealtimeService : IRealtimeUpdateService
    {
        private readonly IHubContext<AppHub> _hubContext;

        public SignalrRealtimeService(IHubContext<AppHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // El nombre del grupo debe coincidir con el del Hub.
        private string GetUserGroupName(string userId) => $"User-{userId}";

        public async Task SendUpdateToUserAsync<T>(string userId, string topic, T payload)
        {
            await _hubContext.Clients
                .Group(GetUserGroupName(userId))
                .SendAsync(topic, payload); // Ej: SendAsync("ReceiveHeaderUpdate", dto)
        }

        public async Task SendUpdateToAllAsync<T>(string topic, T payload)
        {
            await _hubContext.Clients.All.SendAsync(topic, payload);
        }

        public async Task SendUpdateToGroupAsync<T>(string groupName, string topic, T payload)
        {
            await _hubContext.Clients
                .Group(groupName) // La única diferencia: .Group() en lugar de .User()
                .SendAsync(topic, payload);
        }
    }
}