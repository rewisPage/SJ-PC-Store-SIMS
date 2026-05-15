using System;

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
    }
}