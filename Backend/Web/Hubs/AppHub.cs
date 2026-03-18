using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Web.Hubs
{
    /// <summary>
    /// Hub genérico de SignalR para la aplicación.
    /// Su única responsabilidades gestionar conexiones y grupos.
    /// </summary>
    [Authorize]
    public class AppHub : Hub
    {
        // El nombre del grupo se basará en el ID del usuario.
        private string GetUserGroupName(string userId) => $"User-{userId}";

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Agrupamos la conexión de este usuario bajo su propio ID.
                // Esto nos permite enviar mensajes solo a "User-123", por ejemplo.
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Une la conexión actual a un grupo de inventario específico.
        /// </summary>
        /// <param name="inventaryId">El ID del inventario al que se une.</param>
        [Authorize(Roles = "OPERATIVO")] // Solo operativos
        public async Task JoinInventoryGroup(string inventaryId)
        {
            // NOTA: Confiamos en que el POST /join ya validó el permiso.
            // Si quisiéramos doble seguridad, re-validaríamos aquí
            // contra la BD que Context.User tiene permiso para inventaryId.

            string groupName = $"Inventary-{inventaryId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // (Opcional) Notificar al grupo que alguien se unió
             //await Clients.Group(groupName).SendAsync("UserJoined", Context.User.Identity.Name);
        }
    }
}