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
        public int PhysicalStockCount { get; set; }
        public bool IsActive { get; set; }

        // NEW: Audit & History Fields
        public DateTime CreatedTime { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedTime { get; set; }
        public string ModifiedBy { get; set; }
        public int TotalSold { get; set; }
        public int TotalDefective { get; set; }
    }
}