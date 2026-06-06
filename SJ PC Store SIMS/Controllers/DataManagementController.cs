using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class DataManagementController
    {
        public void LogActivity(string userId, string action, string category)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // Added ModuleCategory to the INSERT statement
                    string query = "INSERT INTO ACTIVITY_LOG (UserID, ActionDescription, ModuleCategory) VALUES (@U, @A, @C)";
                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@A", action);
                    cmd.Parameters.AddWithValue("@C", category); // New Category Parameter

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { } // Fails silently to prevent interrupting the main business logic
        }

        public string GenerateNextSupplierID()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT TOP 1 SupplierID FROM SUPPLIER WHERE SupplierID LIKE 'SUP-%' ORDER BY SupplierID DESC", conn);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string lastCode = result.ToString();
                        if (lastCode.StartsWith("SUP-") && int.TryParse(lastCode.Substring(4), out int num))
                        {
                            return "SUP-" + (num + 1).ToString("D4");
                        }
                    }
                }
                catch { }
                return "SUP-1001";
            }
        }

        public List<SupplierModel> GetAllSuppliers(string filter = "Active")
        {
            List<SupplierModel> suppliers = new List<SupplierModel>();
            string query = "SELECT * FROM SUPPLIER";
            if (filter == "Active") query += " WHERE IsActive = 1";
            else if (filter == "Inactive") query += " WHERE IsActive = 0";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        suppliers.Add(new SupplierModel
                        {
                            SupplierID = reader["SupplierID"].ToString(),
                            CompanyName = reader["CompanyName"].ToString(),
                            ContactPerson = reader["ContactPerson"]?.ToString(),
                            ContactNumber = reader["ContactNumber"]?.ToString(),
                            EmailAddress = reader["EmailAddress"]?.ToString(),
                            Address = reader["Address"]?.ToString(),
                            Remarks = reader["Remarks"]?.ToString(),
                            DateRegistered = Convert.ToDateTime(reader["DateRegistered"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return suppliers;
        }

        public bool CreateSupplier(SupplierModel s, string userId)
        {
            if (string.IsNullOrWhiteSpace(s.SupplierID)) s.SupplierID = GenerateNextSupplierID();

            string query = @"INSERT INTO SUPPLIER (SupplierID, CompanyName, ContactPerson, ContactNumber, EmailAddress, Address, Remarks, DateRegistered, IsActive) 
                             VALUES (@ID, @Comp, @CP, @CN, @Email, @Addr, @Rem, GETDATE(), 1)";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", s.SupplierID); cmd.Parameters.AddWithValue("@Comp", s.CompanyName);
                cmd.Parameters.AddWithValue("@CP", s.ContactPerson ?? ""); cmd.Parameters.AddWithValue("@CN", s.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", s.EmailAddress ?? ""); cmd.Parameters.AddWithValue("@Addr", s.Address ?? "");
                cmd.Parameters.AddWithValue("@Rem", s.Remarks ?? "");
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateSupplier(SupplierModel s, string userId)
        {
            string query = @"UPDATE SUPPLIER SET CompanyName=@Comp, ContactPerson=@CP, ContactNumber=@CN, EmailAddress=@Email, Address=@Addr, Remarks=@Rem WHERE SupplierID=@ID";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", s.SupplierID); cmd.Parameters.AddWithValue("@Comp", s.CompanyName);
                cmd.Parameters.AddWithValue("@CP", s.ContactPerson ?? ""); cmd.Parameters.AddWithValue("@CN", s.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", s.EmailAddress ?? ""); cmd.Parameters.AddWithValue("@Addr", s.Address ?? "");
                cmd.Parameters.AddWithValue("@Rem", s.Remarks ?? "");
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeactivateSupplier(string supplierId, string userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("UPDATE SUPPLIER SET IsActive = 0 WHERE SupplierID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", supplierId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<POHistoryModel> GetSupplierPOHistory(string supplierId)
        {
            List<POHistoryModel> history = new List<POHistoryModel>();

            // FIXED: Using GrandTotal instead of TotalCost.
            // FIXED: Summing 'Quantity' from PROCUREMENT_ITEM so it accurately counts items even before Goods Receipt.
            string query = @"
                SELECT p.PO_Number, p.OrderDate, p.GrandTotal, p.Status,
                       (SELECT ISNULL(SUM(Quantity), 0) FROM PROCUREMENT_ITEM pi WHERE pi.PO_Number = p.PO_Number) AS TotalItems
                FROM PROCUREMENT p
                WHERE p.SupplierID = @SupID
                ORDER BY p.OrderDate DESC";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SupID", supplierId);
                conn.Open();
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new POHistoryModel
                            {
                                PO_Number = reader["PO_Number"].ToString(),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                TotalCost = Convert.ToDecimal(reader["GrandTotal"]), // Maps the DB GrandTotal to the Model's TotalCost
                                Status = reader["Status"].ToString(),
                                TotalItems = Convert.ToInt32(reader["TotalItems"])
                            });
                        }
                    }
                }
                catch { } // Safe failure
            }
            return history;
        }

        public bool ActivateSupplier(string supplierId, string userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("UPDATE SUPPLIER SET IsActive = 1 WHERE SupplierID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", supplierId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}