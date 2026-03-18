using Entity.DTOs.System.Connection;
using Entity.Models.System;

namespace Business.Services.Entities.Interfaces.Connection
{
    public interface IInventoryJoinService
    {
        /// <summary>
        /// Valida si un usuario (invitado) puede unirse a un inventario existente.
        /// Lanza ValidationException si no es válido.
        /// </summary>
        /// <param name="request">El DTO con el CodeIvitation</param>
        /// <param name="guestUserId">El UserId del invitado (desde el JWT)</param>
        Task<Inventary> ValidateJoinAsync(JoinInventoryRequestDTO request, int guestUserId);
    }
}
