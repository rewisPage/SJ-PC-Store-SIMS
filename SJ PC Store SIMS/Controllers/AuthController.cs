using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class AuthController
    {
        public AuthController()
        {
            SeedDefaultAdmin(); // Creates admin account if DB is empty
        }

        public UserModel Login(string username, string rawPassword)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Use INNER JOIN to dynamically fetch the Role permissions alongside the user data
                string query = @"
                    SELECT u.UserID, u.FirstName, u.LastName, u.Username, u.PasswordHash, u.Role, u.Status,
                           r.CanManageUsers, r.CanManageInventory, r.CanProcessSales, 
                           r.CanManageProcurement, r.CanViewReports, r.CanManageData
                    FROM [USER] u
                    INNER JOIN [ROLE] r ON u.Role = r.RoleName
                    WHERE u.Username = @Username AND u.Status = 'Active'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();
                            // Verify BCrypt password
                            if (BCrypt.Net.BCrypt.Verify(rawPassword, storedHash))
                            {
                                return new UserModel
                                {
                                    UserID = reader["UserID"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Username = reader["Username"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    Status = reader["Status"].ToString(),

                                    // Map the Database Booleans to the Permissions object
                                    Permissions = new RolePermissions
                                    {
                                        CanManageUsers = Convert.ToBoolean(reader["CanManageUsers"]),
                                        CanManageInventory = Convert.ToBoolean(reader["CanManageInventory"]),
                                        CanProcessSales = Convert.ToBoolean(reader["CanProcessSales"]),
                                        CanManageProcurement = Convert.ToBoolean(reader["CanManageProcurement"]),
                                        CanViewReports = Convert.ToBoolean(reader["CanViewReports"]),
                                        CanManageData = Convert.ToBoolean(reader["CanManageData"])
                                    }
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool VerifyPasskey(string username, string passkey)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM [USER] WHERE Username = @Username AND Passkey = @Passkey AND Status = 'Active'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Passkey", passkey);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private void SeedDefaultAdmin()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Ensure the Administrator role exists first to satisfy the Foreign Key
                string checkRoleQuery = "SELECT COUNT(1) FROM [ROLE] WHERE RoleName = 'Administrator'";
                using (SqlCommand checkRoleCmd = new SqlCommand(checkRoleQuery, conn))
                {
                    if (Convert.ToInt32(checkRoleCmd.ExecuteScalar()) == 0)
                    {
                        string insertRoleQuery = "INSERT INTO [ROLE] (RoleName, CanManageUsers, CanManageInventory, CanProcessSales, CanManageProcurement, CanViewReports, CanManageData) VALUES ('Administrator', 1, 1, 1, 1, 1, 1)";
                        using (SqlCommand insertRoleCmd = new SqlCommand(insertRoleQuery, conn)) { insertRoleCmd.ExecuteNonQuery(); }
                    }
                }

                string checkQuery = "SELECT COUNT(1) FROM [USER]";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                    {
                        string insertQuery = @"INSERT INTO [USER] (UserID, FirstName, LastName, ContactNumber, Username, PasswordHash, Role, Passkey, Status, CreatedTime) 
                                       VALUES (@ID, 'System', 'Admin', '00000000000', 'admin', @Hash, 'Administrator', 'A1B2C3', 'Active', GETDATE())";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@ID", "USR-" + DateTime.Now.Ticks.ToString().Substring(0, 8));
                            insertCmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword("admin123"));
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}