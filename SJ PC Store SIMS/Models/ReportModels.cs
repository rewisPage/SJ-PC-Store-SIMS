namespace SJ_PC_Store_SIMS.Models
{
    public class SalesReportModel
    {
        public string ReceiptID { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; }
    }

    public class ProcurementReportModel
    {
        public string PO_Number { get; set; }
        public DateTime OrderDate { get; set; }
        public string SupplierName { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; }
    }

    public class InventoryReportModel
    {
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string Specs { get; set; }
        public int AvailableStock { get; set; }
        public decimal UnitValue { get; set; }
        public decimal TotalAssetValue => AvailableStock * UnitValue; // Auto-calculated
    }

    public class StockReportModel
    {
        public string SerialNumber { get; set; }
        public string ItemCode { get; set; }
        public string Specs { get; set; }
        public string Status { get; set; }
        public string PO_Number { get; set; }
        public string SupplierName { get; set; }
    }
}