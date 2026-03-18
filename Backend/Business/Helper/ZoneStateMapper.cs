using Entity.DTOs.System.Zone;
using Entity.Models.System;
using Utilities.Enums.Models;

namespace Business.Helper
{
    public static class ZoneStateMapper
    {
        public static ZoneStateUpdateDTO Map(Zone zone)
        {
            var newState = zone.StateZone;
            string newStateLabel = "";
            string newIconName = "";
            bool isAvailable = false;

            switch (newState)
            {
                case StateZone.InInventory:
                    newStateLabel = "En Inventario";
                    newIconName = "lock-close-outline";
                    isAvailable = false;
                    break;
                case StateZone.InVerification:
                    newStateLabel = "En Verificación";
                    newIconName = "shield-checkmark-outline";
                    isAvailable = false;
                    break;
                case StateZone.Available:
                default:
                    newStateLabel = "Disponible";
                    newIconName = "lock-open-outline";
                    isAvailable = true;
                    break;
            }

            return new ZoneStateUpdateDTO
            {
                ZoneId = zone.Id,
                NewState = newState.ToString(),
                NewStateLabel = newStateLabel,
                NewIconName = newIconName,
                IsAvailable = isAvailable
            };
        }
    }
}
