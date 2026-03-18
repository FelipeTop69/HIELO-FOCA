using Business.Services.Entities.Interfaces.Connection;
using Data.Factory;
using Data.Repository.Interfaces.Specific.System;
using Entity.DTOs.System.Connection;
using Entity.Models.System;
using Utilities.Enums.Models;
using Utilities.Exceptions;

namespace Business.Services.Entities.Implementations.Connection
{
    public class InventoryJoinService : IInventoryJoinService
    {
        private readonly IInventary _inventoryRepo;
        private readonly IOperating _operatingRepo;

        public InventoryJoinService(IDataFactoryGlobal factory)
        {
            _inventoryRepo = factory.CreateInventaryData();
            _operatingRepo = factory.CreateOperatingData();
        }

        public async Task<Inventary> ValidateJoinAsync(JoinInventoryRequestDTO request, int guestUserId)
        {
            if (string.IsNullOrEmpty(request.InvitationCode))
            {
                throw new ValidationException("El código de invitación no puede estar vacío.");
            }

            // Obtener el Inventario por el CÓDIGO DE INVITACIÓN
            var inventary = await _inventoryRepo.GetByInvitationCodeAsync(request.InvitationCode);

            if (inventary == null || !inventary.Active)
            {
                throw new ValidationException($"El código de invitación '{request.InvitationCode}' no es válido.");
            }

            if (inventary.Status != InventaryStatus.InProgress)
            {
                // Si no esta en progreso, rechazar la union.
                throw new ValidationException("Este inventario ya ha finalizado y no acepta nuevos miembros.");
            }

            var requiredGroupId = inventary.OperatingGroupId;

            // Obtener el OperatingGroupId al que pertenece el INVITADO
            var guestOperating = await _operatingRepo.GetByUserIdAsync(guestUserId);

            if (guestOperating == null)
            {
                throw new ValidationException("Tu usuario no es un operativo válido o no está activo.");
            }

            // Validar si el invitado tiene un grupo asignado
            var guestGroupId = guestOperating.OperationalGroupId;

            if (!guestGroupId.HasValue)
            {
                throw new ValidationException("No estás asignado a ningún grupo de trabajo.");
            }

            // La validacion final
            if (guestGroupId.Value != requiredGroupId)
            {
                throw new ValidationException("No perteneces al grupo operativo asignado a este inventario.");
            }

            return inventary;
        }
    }
}
