namespace Entity.DTOs.System.Zone
{
    public class ZoneStateUpdateDTO
    {
        public int ZoneId { get; set; }
        public string NewState { get; set; } = string.Empty;
        public string NewStateLabel { get; set; } = string.Empty;
        public string NewIconName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}
