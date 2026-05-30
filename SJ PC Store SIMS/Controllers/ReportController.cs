using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class ReportController
    {
        // ==========================================
        // 1. SALES REPORT FETCHING
        // ==========================================
        public List<SalesReportModel> GetFilteredSales(DateTime fromDate, DateTime toDate, string statusFilter, string searchKeyword)
        {
            var list = new List<SalesReportModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                // Adjust toDate to include the entire end day (up to 11:59 PM)
                DateTime adjustedToDate = toDate.Date.AddDays(1).AddTicks(-1);

                string query = @"SELECT ReceiptID, OrderDate, CustomerName, SubTotal, Discount, Tax, GrandTotal, Status 
                                 FROM [TRANSACTION] 
                                 WHERE OrderDate >= @From AND OrderDate <= @To";

                if (statusFilter != "All" && !string.IsNullOrWhiteSpace(statusFilter))
                    query += " AND Status = @Status";

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    query += " AND (ReceiptID LIKE @Search OR CustomerName LIKE @Search)";

                query += " ORDER BY OrderDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@From", fromDate.Date);
                cmd.Parameters.AddWithValue("@To", adjustedToDate);

                if (statusFilter != "All") cmd.Parameters.AddWithValue("@Status", statusFilter);
                if (!string.IsNullOrWhiteSpace(searchKeyword)) cmd.Parameters.AddWithValue("@Search", "%" + searchKeyword + "%");

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SalesReportModel
                        {
                            ReceiptID = reader["ReceiptID"].ToString(),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            CustomerName = reader["CustomerName"].ToString(),
                            SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                            Discount = Convert.ToDecimal(reader["Discount"]),
                            Tax = Convert.ToDecimal(reader["Tax"]),
                            GrandTotal = Convert.ToDecimal(reader["GrandTotal"]),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        // ==========================================
        // 2. PROCUREMENT REPORT FETCHING
        // ==========================================
        public List<ProcurementReportModel> GetFilteredProcurement(DateTime fromDate, DateTime toDate, string statusFilter, string searchKeyword)
        {
            var list = new List<ProcurementReportModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                DateTime adjustedToDate = toDate.Date.AddDays(1).AddTicks(-1);

                string query = @"SELECT PO_Number, OrderDate, SupplierID, SubTotal, GrandTotal, Status 
                                 FROM PROCUREMENT 
                                 WHERE OrderDate >= @From AND OrderDate <= @To";

                if (statusFilter != "All" && !string.IsNullOrWhiteSpace(statusFilter))
                    query += " AND Status = @Status";

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    query += " AND (PO_Number LIKE @Search OR SupplierID LIKE @Search)";

                query += " ORDER BY OrderDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@From", fromDate.Date);
                cmd.Parameters.AddWithValue("@To", adjustedToDate);

                if (statusFilter != "All") cmd.Parameters.AddWithValue("@Status", statusFilter);
                if (!string.IsNullOrWhiteSpace(searchKeyword)) cmd.Parameters.AddWithValue("@Search", "%" + searchKeyword + "%");

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ProcurementReportModel
                        {
                            PO_Number = reader["PO_Number"].ToString(),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            SupplierName = reader["SupplierID"].ToString(),
                            SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                            GrandTotal = Convert.ToDecimal(reader["GrandTotal"]),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        // ==========================================
        // 3. INVENTORY REPORT FETCHING
        // ==========================================
        public List<InventoryReportModel> GetFilteredInventory(string categoryFilter, string searchKeyword)
        {
            var list = new List<InventoryReportModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                // We use a subquery to count only the physical stock marked as 'Available'
                string query = @"SELECT I.ItemCode, I.Category, I.Specs, I.CurrentValue,
                                 (SELECT COUNT(*) FROM STOCK_INSTANCE S WHERE S.ItemCode = I.ItemCode AND S.Status = 'Available') AS AvailableStock
                                 FROM ITEM_MASTER I 
                                 WHERE I.IsActive = 1";

                if (categoryFilter != "All Categories" && !string.IsNullOrWhiteSpace(categoryFilter))
                    query += " AND I.Category = @Category";

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    query += " AND (I.ItemCode LIKE @Search OR I.Specs LIKE @Search)";

                query += " ORDER BY I.Category, I.ItemCode";

                SqlCommand cmd = new SqlCommand(query, conn);
                if (categoryFilter != "All Categories") cmd.Parameters.AddWithValue("@Category", categoryFilter);
                if (!string.IsNullOrWhiteSpace(searchKeyword)) cmd.Parameters.AddWithValue("@Search", "%" + searchKeyword + "%");

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new InventoryReportModel
                        {
                            ItemCode = reader["ItemCode"].ToString(),
                            Category = reader["Category"].ToString(),
                            Specs = reader["Specs"].ToString(),
                            AvailableStock = Convert.ToInt32(reader["AvailableStock"]),
                            UnitValue = Convert.ToDecimal(reader["CurrentValue"])
                        });
                    }
                }
            }
            return list;
        }

        // ==========================================
        // 4. STOCKS REPORT FETCHING
        // ==========================================
        public List<StockReportModel> GetFilteredStocks(string statusFilter, string searchKeyword)
        {
            var list = new List<StockReportModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"SELECT S.SerialNumber, S.ItemCode, I.Specs, S.Status, S.PO_Number, 
                                        COALESCE(SUP_DIRECT.CompanyName, SUP_PO.CompanyName) AS SupplierName
                                 FROM STOCK_INSTANCE S
                                 LEFT JOIN ITEM_MASTER I ON S.ItemCode = I.ItemCode
                                 LEFT JOIN SUPPLIER SUP_DIRECT ON S.SupplierID = SUP_DIRECT.SupplierID
                                 LEFT JOIN PROCUREMENT P ON S.PO_Number = P.PO_Number
                                 LEFT JOIN SUPPLIER SUP_PO ON P.SupplierID = SUP_PO.SupplierID
                                 WHERE 1 = 1";

                if (statusFilter != "All" && !string.IsNullOrWhiteSpace(statusFilter))
                    query += " AND S.Status = @Status";

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    query += " AND (S.SerialNumber LIKE @Search OR S.ItemCode LIKE @Search OR I.Specs LIKE @Search)";

                query += " ORDER BY S.ItemCode, S.SerialNumber";

                SqlCommand cmd = new SqlCommand(query, conn);
                if (statusFilter != "All") cmd.Parameters.AddWithValue("@Status", statusFilter);
                if (!string.IsNullOrWhiteSpace(searchKeyword)) cmd.Parameters.AddWithValue("@Search", "%" + searchKeyword + "%");

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new StockReportModel
                        {
                            SerialNumber = reader["SerialNumber"].ToString(),
                            ItemCode = reader["ItemCode"].ToString(),
                            Specs = reader["Specs"].ToString(),
                            Status = reader["Status"].ToString(),
                            // Handle potential nulls for incoming stock not tied to a PO
                            PO_Number = reader["PO_Number"] != DBNull.Value ? reader["PO_Number"].ToString() : "N/A",
                            SupplierName = reader["SupplierName"] != DBNull.Value ? reader["SupplierName"].ToString() : "N/A"
                        });
                    }
                }
            }
            return list;
        }
    }
}