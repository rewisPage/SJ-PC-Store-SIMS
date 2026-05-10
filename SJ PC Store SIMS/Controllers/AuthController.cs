using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System;

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
                string query = "SELECT UserID, FirstName, LastName, Username, PasswordHash, Role, Status FROM [USER] WHERE Username = @Username AND Status = 'Active'";

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
                                    Status = reader["Status"].ToString()
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
                string checkQuery = "SELECT COUNT(1) FROM [USER]";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                    {
                        string insertQuery = @"INSERT INTO [USER] (UserID, FirstName, LastName, ContactNumber, Username, PasswordHash, Role, Passkey, Status) 
                                               VALUES (@ID, 'System', 'Admin', '00000000000', 'admin', @Hash, 'Administrator', 'A1B2C3', 'Active')";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@ID", "USR-" + DateTime.Now.Ticks.ToString().Substring(0, 8));
                            insertCmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword("admin123")); // Default password is admin123
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}