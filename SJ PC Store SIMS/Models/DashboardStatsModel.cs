namespace SJ_PC_Store_SIMS.Models
{
    public class DashboardStatsModel
    {
        // Sales Management
        public decimal TodaysRevenue { get; set; }
        public int TransactionsToday { get; set; }

        // Inventory Management
        public decimal TotalStockValue { get; set; }
        public int LowStockAlerts { get; set; }
        public int TotalProducts { get; set; }

        // Procurement Management
        public int PendingProcurements { get; set; }
        public int TotalPurchaseOrders { get; set; }

        // Data Management
        public int TotalSuppliers { get; set; }

        // User Management
        public int TotalActiveUsers { get; set; }

        public Dictionary<string, double> WeeklySalesData { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> InventoryCategoryData { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, double> StockStatusData { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> ProcurementExpenseData { get; set; } = new Dictionary<string, double>();

        // Top Items Sold
        public List<TopItemModel> TopItemsSold { get; set; } = new List<TopItemModel>();

        // Low Stock Items
        public List<LowStockItemModel> LowStockItems { get; set; } = new List<LowStockItemModel>();
    }

    public class TopItemModel
    {
        public int Rank { get; set; }
        public string ItemCode { get; set; }
        public string ItemSpecs { get; set; }
        public int UnitsSold { get; set; }
    }

    public class LowStockItemModel
    {
        public int Rank { get; set; }
        public string ItemCode { get; set; }
        public string ItemSpecs { get; set; }
        public int AvailableStock { get; set; }
    }
}