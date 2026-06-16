using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class DashboardController
    {
        public List<ActivityLogModel> GetUserRecentActivity(string userId, int limit = 15)
        {
            List<ActivityLogModel> logs = new List<ActivityLogModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                // Queries the latest logs for this specific user
                string query = @"
                    SELECT TOP (@Limit) LogID, UserID, ModuleCategory, ActionDescription, LogDate 
                    FROM ACTIVITY_LOG 
                    WHERE UserID = @UserID 
                    ORDER BY LogDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Limit", limit);
                cmd.Parameters.AddWithValue("@UserID", userId);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new ActivityLogModel
                            {
                                LogID = Convert.ToInt32(reader["LogID"]),
                                UserID = reader["UserID"].ToString(),
                                ModuleCategory = reader["ModuleCategory"]?.ToString() ?? "System",
                                ActionDescription = reader["ActionDescription"].ToString(),
                                LogDate = Convert.ToDateTime(reader["LogDate"])
                            });
                        }
                    }
                }
                catch { /* Fail silently to prevent crashing the dashboard */ }
            }
            return logs;
        }

        public DashboardStatsModel GetDashboardStatistics()
        {
            DashboardStatsModel stats = new DashboardStatsModel();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // CHANGED: Added "AND Status = 'Completed'" so cancelled orders and quotations are ignored
                string queryRevenue = @"SELECT ISNULL(SUM(GrandTotal), 0) FROM [TRANSACTION] WHERE CONVERT(date, OrderDate) = CONVERT(date, GETDATE()) AND Status = 'Completed'";
                using (SqlCommand cmd = new SqlCommand(queryRevenue, conn)) { stats.TodaysRevenue = Convert.ToDecimal(cmd.ExecuteScalar()); }

                // CHANGED: Added "AND Status = 'Completed'" so the dashboard transaction count perfectly matches the revenue
                string queryTransactions = @"SELECT COUNT(ReceiptID) FROM [TRANSACTION] WHERE CONVERT(date, OrderDate) = CONVERT(date, GETDATE()) AND Status = 'Completed'";
                using (SqlCommand cmd = new SqlCommand(queryTransactions, conn)) { stats.TransactionsToday = Convert.ToInt32(cmd.ExecuteScalar()); }
                string queryStockValue = @"SELECT ISNULL(SUM(m.CurrentValue), 0) FROM STOCK_INSTANCE s JOIN ITEM_MASTER m ON s.ItemCode = m.ItemCode WHERE s.Status = 'Available'";
                using (SqlCommand cmd = new SqlCommand(queryStockValue, conn)) { stats.TotalStockValue = Convert.ToDecimal(cmd.ExecuteScalar()); }

                // FIX: Added query to count Total Registered Active Blueprints
                string queryTotalProducts = "SELECT COUNT(ItemCode) FROM ITEM_MASTER WHERE IsActive = 1";
                using (SqlCommand cmd = new SqlCommand(queryTotalProducts, conn)) { stats.TotalProducts = Convert.ToInt32(cmd.ExecuteScalar()); }

                // FIX: Added query to count Low Stock Alerts (Blueprints with 2 or less available stock)
                string queryLowStock = @"SELECT COUNT(*) FROM ( SELECT i.ItemCode, (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status='Available') as Qty FROM ITEM_MASTER i WHERE i.IsActive = 1 ) as StockTable WHERE Qty <= 5";
                using (SqlCommand cmd = new SqlCommand(queryLowStock, conn)) { stats.LowStockAlerts = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryPending = @"SELECT COUNT(PO_Number) FROM [PROCUREMENT] WHERE Status = 'Ordered'";
                using (SqlCommand cmd = new SqlCommand(queryPending, conn)) { stats.PendingProcurements = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryTotalPO = "SELECT COUNT(PO_Number) FROM [PROCUREMENT]";
                using (SqlCommand cmd = new SqlCommand(queryTotalPO, conn)) { stats.TotalPurchaseOrders = Convert.ToInt32(cmd.ExecuteScalar()); }

                string querySuppliers = "SELECT COUNT(SupplierID) FROM [SUPPLIER]";
                using (SqlCommand cmd = new SqlCommand(querySuppliers, conn)) { stats.TotalSuppliers = Convert.ToInt32(cmd.ExecuteScalar()); }

                string queryUsers = "SELECT COUNT(UserID) FROM [USER] WHERE Status = 'Active'";
                using (SqlCommand cmd = new SqlCommand(queryUsers, conn)) { stats.TotalActiveUsers = Convert.ToInt32(cmd.ExecuteScalar()); }

                // --- NEW CHART DATA QUERIES ---

                // 1. Fetch data for the Sales Bar Chart (Last 7 Days Revenue)
                // We use Status = 'Completed' based on your transaction modifications
                string queryWeeklySales = @"
                    SELECT TOP 7 CONVERT(date, OrderDate) as SaleDate, SUM(GrandTotal) as DailyTotal 
                    FROM [TRANSACTION] 
                    WHERE Status = 'Completed' 
                    GROUP BY CONVERT(date, OrderDate) 
                    ORDER BY SaleDate ASC";

                using (SqlCommand cmd = new SqlCommand(queryWeeklySales, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateLabel = Convert.ToDateTime(reader["SaleDate"]).ToString("MMM dd");
                            double total = Convert.ToDouble(reader["DailyTotal"]);
                            stats.WeeklySalesData.Add(dateLabel, total);
                        }
                    }
                }

                // 2. Fetch data for the Inventory Donut Chart (Stock count by Category)
                // Groups STOCK_INSTANCE records by the item's Category to show total physical stock per category
                string queryInvCategories = @"
                    SELECT TOP 5 m.Category, COUNT(s.SerialNumber) as StockCount 
                    FROM STOCK_INSTANCE s
                    JOIN ITEM_MASTER m ON s.ItemCode = m.ItemCode
                    WHERE m.IsActive = 1 AND s.Status = 'Available'
                    GROUP BY m.Category 
                    ORDER BY StockCount DESC";

                using (SqlCommand cmd = new SqlCommand(queryInvCategories, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Category can be null, so we handle it safely
                            string cat = reader["Category"]?.ToString() ?? "Uncategorized";
                            double count = Convert.ToDouble(reader["StockCount"]);
                            stats.InventoryCategoryData.Add(cat, count);
                        }
                    }
                }

                // 3. Fetch data for Stock Status Pie Chart (Available, Sold, Defective, Returned)
                string queryStockStatus = "SELECT Status, COUNT(SerialNumber) as StatusCount FROM STOCK_INSTANCE GROUP BY Status";
                using (SqlCommand cmd = new SqlCommand(queryStockStatus, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.StockStatusData.Add(reader["Status"].ToString(), Convert.ToDouble(reader["StatusCount"]));
                        }
                    }
                }

                // 4. Fetch data for Procurement Expense Bar Chart (Last 7 Days)
                string queryProcurement = @"
                    SELECT TOP 7 CONVERT(date, OrderDate) as OrderDay, ISNULL(SUM(GrandTotal), 0) as DailyTotal 
                    FROM [PROCUREMENT] 
                    WHERE Status NOT IN ('Draft', 'Cancelled')
                    GROUP BY CONVERT(date, OrderDate) 
                    ORDER BY OrderDay ASC";

                using (SqlCommand cmd = new SqlCommand(queryProcurement, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateLabel = Convert.ToDateTime(reader["OrderDay"]).ToString("MMM dd");
                            stats.ProcurementExpenseData.Add(dateLabel, Convert.ToDouble(reader["DailyTotal"]));
                        }
                    }
                }

                // 5. Fetch Top 3 Items Sold (All-time or this month)
                string queryTopItems = @"
                    SELECT TOP 3 
                        ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) as Rank,
                        m.ItemCode,
                        m.Specs,
                        COUNT(*) as UnitsSold
                    FROM TRANSACTION_ITEM ti
                    JOIN STOCK_INSTANCE s ON ti.SerialNumber = s.SerialNumber
                    JOIN ITEM_MASTER m ON s.ItemCode = m.ItemCode
                    GROUP BY m.ItemCode, m.Specs
                    ORDER BY UnitsSold DESC";

                using (SqlCommand cmd = new SqlCommand(queryTopItems, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (reader.Read())
                        {
                            stats.TopItemsSold.Add(new TopItemModel
                            {
                                Rank = rank,
                                ItemCode = reader["ItemCode"].ToString(),
                                ItemSpecs = reader["Specs"].ToString(),
                                UnitsSold = Convert.ToInt32(reader["UnitsSold"])
                            });
                            rank++;
                        }
                    }
                }

                // 6. Fetch Low Stock Items (Items with 2 or less available stock)
                string queryLowStockItems = @"
                    SELECT TOP 10
                        ROW_NUMBER() OVER (ORDER BY Qty ASC) as Rank,
                        StockTable.ItemCode,
                        m.Specs,
                        StockTable.Qty as AvailableStock
                    FROM (
                        SELECT i.ItemCode, (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status='Available') as Qty
                        FROM ITEM_MASTER i 
                        WHERE i.IsActive = 1
                    ) as StockTable
                    JOIN ITEM_MASTER m ON StockTable.ItemCode = m.ItemCode
                    WHERE Qty <= 5
                    ORDER BY Qty ASC";

                using (SqlCommand cmd = new SqlCommand(queryLowStockItems, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (reader.Read())
                        {
                            stats.LowStockItems.Add(new LowStockItemModel
                            {
                                Rank = rank,
                                ItemCode = reader["ItemCode"].ToString(),
                                ItemSpecs = reader["Specs"].ToString(),
                                AvailableStock = Convert.ToInt32(reader["AvailableStock"])
                            });
                            rank++;
                        }
                    }
                }
            }

            return stats;
        }
    }
}