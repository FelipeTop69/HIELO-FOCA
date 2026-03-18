namespace Entity.DTOs.ScanItem.Missing
{
    public class MissingItemDTO
    {
        public int ItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
    }
}
