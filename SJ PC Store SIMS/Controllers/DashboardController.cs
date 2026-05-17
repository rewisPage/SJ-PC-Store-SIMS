using System;
using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class DashboardController
    {
        public DashboardStatsModel GetDashboardStatistics()
        {
            DashboardStatsModel stats = new DashboardStatsModel();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string queryRevenue = @"SELECT ISNULL(SUM(GrandTotal), 0) FROM [TRANSACTION] WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(queryRevenue, conn)) { stats.TodaysRevenue = Convert.ToDecimal(cmd.ExecuteScalar()); }

                string queryTransactions = @"SELECT COUNT(ReceiptID) FROM [TRANSACTION] WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(queryTransactions, conn)) { stats.TransactionsToday = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryStockValue = @"SELECT ISNULL(SUM(m.CurrentValue), 0) FROM STOCK_INSTANCE s JOIN ITEM_MASTER m ON s.ItemCode = m.ItemCode WHERE s.Status = 'Available'";
                using (SqlCommand cmd = new SqlCommand(queryStockValue, conn)) { stats.TotalStockValue = Convert.ToDecimal(cmd.ExecuteScalar()); }

                // FIX: Added query to count Total Registered Active Blueprints
                string queryTotalProducts = "SELECT COUNT(ItemCode) FROM ITEM_MASTER WHERE IsActive = 1";
                using (SqlCommand cmd = new SqlCommand(queryTotalProducts, conn)) { stats.TotalProducts = Convert.ToInt32(cmd.ExecuteScalar()); }

                // FIX: Added query to count Low Stock Alerts (Blueprints with 2 or less available stock)
                string queryLowStock = @"SELECT COUNT(*) FROM ( SELECT i.ItemCode, (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status='Available') as Qty FROM ITEM_MASTER i WHERE i.IsActive = 1 ) as StockTable WHERE Qty <= 2";
                using (SqlCommand cmd = new SqlCommand(queryLowStock, conn)) { stats.LowStockAlerts = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryPending = @"SELECT COUNT(PO_Number) FROM [PROCUREMENT] WHERE Status = 'Pending'";
                using (SqlCommand cmd = new SqlCommand(queryPending, conn)) { stats.PendingProcurements = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryTotalPO = "SELECT COUNT(PO_Number) FROM [PROCUREMENT]";
                using (SqlCommand cmd = new SqlCommand(queryTotalPO, conn)) { stats.TotalPurchaseOrders = Convert.ToInt32(cmd.ExecuteScalar()); }

                string querySuppliers = "SELECT COUNT(SupplierID) FROM [SUPPLIER]";
                using (SqlCommand cmd = new SqlCommand(querySuppliers, conn)) { stats.TotalSuppliers = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryUsers = "SELECT COUNT(UserID) FROM [USER] WHERE Status = 'Active'";
                using (SqlCommand cmd = new SqlCommand(queryUsers, conn)) { stats.TotalActiveUsers = Convert.ToInt32(cmd.ExecuteScalar()); }
            }
            return stats;
        }
    }
}