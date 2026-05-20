using System;

namespace SJ_PC_Store_SIMS.Models
{
    public class SupplierModel
    {
        public string SupplierID { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
        public string Address { get; set; }
        public string Remarks { get; set; }
        public DateTime DateRegistered { get; set; }
        public bool IsActive { get; set; }
    }

    public class POHistoryModel
    {
        public string PO_Number { get; set; }
        public DateTime OrderDate { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; }
    }
}