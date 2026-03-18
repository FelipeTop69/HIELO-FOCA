namespace Entity.DTOs.System.Verification
{
    public class VerificationListUpdateDTO
    {
        public int InventaryId { get; set; }
        public DateTime Date { get; set; }
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int BranchId { get; set; } 

        // El tipo de acción: "Added" o "Removed"
        public string UpdateType { get; set; } = string.Empty;
    }
}
