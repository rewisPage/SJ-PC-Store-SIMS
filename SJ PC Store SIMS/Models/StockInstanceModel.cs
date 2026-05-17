using System;

namespace SJ_PC_Store_SIMS.Models
{
    public class StockInstanceModel
    {
        public string SerialNumber { get; set; }
        public string ItemCode { get; set; }
        public string PO_Number { get; set; }
        public string SupplierID { get; set; }
        public string Status { get; set; }
        public string DefectReason { get; set; } // Added to track damage history
    }
}