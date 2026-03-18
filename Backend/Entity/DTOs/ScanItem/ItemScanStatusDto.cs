using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.DTOs.ScanItem
{
    public class ItemScanStatusDto
    {
        public bool IsScanned { get; set; }
        public int? ItemId { get; set; }
        public string? Message { get; set; }
    }
}
