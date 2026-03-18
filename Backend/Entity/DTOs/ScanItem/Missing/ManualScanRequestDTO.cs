namespace Entity.DTOs.ScanItem.Missing
{
    public class ManualScanRequestDTO
    {
        public int InventaryId { get; set; }
        public List<ManualItemEntryDTO> Items { get; set; } = [];
    }
}
