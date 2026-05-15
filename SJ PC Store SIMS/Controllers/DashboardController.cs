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

                // --- SALES OVERVIEW ---
                string queryRevenue = @"SELECT ISNULL(SUM(GrandTotal), 0) FROM [TRANSACTION] 
                                        WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(queryRevenue, conn))
                {
                    stats.TodaysRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                string queryTransactions = @"SELECT COUNT(ReceiptID) FROM [TRANSACTION] 
                                             WHERE CONVERT(date, SaleDate) = CONVERT(date, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(queryTransactions, conn))
                {
                    stats.TransactionsToday = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // --- INVENTORY OVERVIEW ---
                string queryStockValue = @"SELECT ISNULL(SUM(CurrentValue), 0) FROM [PRODUCT] 
                                           WHERE Status = 'Available'";
                using (SqlCommand cmd = new SqlCommand(queryStockValue, conn))
                {
                    stats.TotalStockValue = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                string queryLowStock = @"SELECT COUNT(*) FROM (
                                            SELECT Category, COUNT(*) as Qty FROM [PRODUCT] 
                                            WHERE Status = 'Available' 
                                            GROUP BY Category 
                                            HAVING COUNT(*) < 5
                                         ) as LowStockCategories";
                using (SqlCommand cmd = new SqlCommand(queryLowStock, conn))
                {
                    stats.LowStockAlerts = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string queryTotalProducts = "SELECT COUNT(SerialNumber) FROM [PRODUCT]";
                using (SqlCommand cmd = new SqlCommand(queryTotalProducts, conn))
                {
                    stats.TotalProducts = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // --- PROCUREMENT OVERVIEW ---
                string queryPending = @"SELECT COUNT(PO_Number) FROM [PROCUREMENT] 
                                        WHERE OrderDate >= DATEADD(day, -7, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(queryPending, conn))
                {
                    stats.PendingProcurements = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string queryTotalPO = "SELECT COUNT(PO_Number) FROM [PROCUREMENT]";
                using (SqlCommand cmd = new SqlCommand(queryTotalPO, conn))
                {
                    stats.TotalPurchaseOrders = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // --- DATA MANAGEMENT OVERVIEW ---
                string querySuppliers = "SELECT COUNT(SupplierID) FROM [SUPPLIER]";
                using (SqlCommand cmd = new SqlCommand(querySuppliers, conn))
                {
                    stats.TotalSuppliers = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // --- USER MANAGEMENT OVERVIEW ---
                string queryUsers = "SELECT COUNT(UserID) FROM [USER] WHERE Status = 'Active'";
                using (SqlCommand cmd = new SqlCommand(queryUsers, conn))
                {
                    stats.TotalActiveUsers = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            return stats;
        }
    }
}