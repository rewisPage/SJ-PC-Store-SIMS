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

                // --- SALES OVERVIEW (Compatible with new schema) ---
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

                // --- INVENTORY OVERVIEW (UPDATED FOR TWO-TIER SYSTEM) ---
                // We join the physical stock (Available) with the Blueprint to get the CurrentValue
                string queryStockValue = @"
                    SELECT ISNULL(SUM(m.CurrentValue), 0) 
                    FROM STOCK_INSTANCE s 
                    JOIN ITEM_MASTER m ON s.ItemCode = m.ItemCode 
                    WHERE s.Status = 'Available'";
                using (SqlCommand cmd = new SqlCommand(queryStockValue, conn))
                {
                    stats.TotalStockValue = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // --- PROCUREMENT OVERVIEW (UPDATED FOR P2P LOGIC) ---
                string queryPending = @"SELECT COUNT(PO_Number) FROM [PROCUREMENT] WHERE Status = 'Pending'";
                using (SqlCommand cmd = new SqlCommand(queryPending, conn))
                {
                    stats.PendingProcurements = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string queryTotalPO = "SELECT COUNT(PO_Number) FROM [PROCUREMENT]";
                using (SqlCommand cmd = new SqlCommand(queryTotalPO, conn))
                {
                    stats.TotalPurchaseOrders = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // --- DATA MANAGEMENT & USER OVERVIEW ---
                string querySuppliers = "SELECT COUNT(SupplierID) FROM [SUPPLIER]";
                using (SqlCommand cmd = new SqlCommand(querySuppliers, conn))
                {
                    stats.TotalSuppliers = Convert.ToInt32(cmd.ExecuteScalar());
                }

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