using System;
using System.Collections.Generic;

namespace SJ_PC_Store_SIMS.Models
{
    public class ProcurementModel
    {
        public string PO_Number { get; set; }
        public string SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDate { get; set; }
        public string Status { get; set; } // Draft, Pending Approval, Ordered, Completed, Cancelled
        public string CreatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string Remarks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public List<ProcurementItemModel> Items { get; set; } = new List<ProcurementItemModel>();
    }

    public class ProcurementItemModel
    {
        public int ItemID { get; set; }
        public string PO_Number { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;
    }
}