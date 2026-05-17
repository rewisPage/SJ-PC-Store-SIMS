using System;

namespace SJ_PC_Store_SIMS.Models
{
    public class ItemMasterModel
    {
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string Specs { get; set; }
        public string ItemCondition { get; set; }
        public decimal BaselineCost { get; set; }
        public decimal CurrentValue { get; set; }
        public int PhysicalStockCount { get; set; } // Calculated field
        public bool IsActive { get; set; } // Added for Soft Deletion
    }
}