namespace SJ_PC_Store_SIMS.Models
{
    public class SalesTransactionModel
    {
        public string ReceiptID { get; set; }
        public string CustomerName { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal ChangeAmount => AmountReceived >= GrandTotal ? AmountReceived - GrandTotal : 0;
        public string Status { get; set; }
        public int WarrantyDays { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string Remarks { get; set; }
        public List<SalesItemModel> Items { get; set; } = new List<SalesItemModel>();
    }

    public class SalesItemModel
    {
        public string SerialNumber { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string ItemCondition { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;
    }
}