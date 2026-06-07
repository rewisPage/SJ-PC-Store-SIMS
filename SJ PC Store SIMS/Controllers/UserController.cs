using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class UserController
    {
        // 1. Updated Activity Logging
        public void LogActivity(string userId, string action, string category)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    string query = "INSERT INTO ACTIVITY_LOG (UserID, ActionDescription, ModuleCategory) VALUES (@U, @A, @C)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@A", action);
                    cmd.Parameters.AddWithValue("@C", category);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // 2. Fetch All Active Roles for ComboBoxes
        public List<string> GetAllRoles()
        {
            List<string> roles = new List<string>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT RoleName FROM [ROLE] WHERE IsActive = 1", conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) { roles.Add(reader["RoleName"].ToString()); }
                }
            }
            return roles;
        }

        public bool CreateRole(string roleName, RolePermissions perms, string currentUserId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
            INSERT INTO [ROLE] (RoleName, CanManageUsers, CanManageInventory, CanProcessSales, CanManageProcurement, CanViewReports, CanManageData, IsActive)
            VALUES (@Name, @Users, @Inv, @Sales, @Proc, @Rep, @Data, 1)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", roleName);
                cmd.Parameters.AddWithValue("@Users", perms.CanManageUsers);
                cmd.Parameters.AddWithValue("@Inv", perms.CanManageInventory);
                cmd.Parameters.AddWithValue("@Sales", perms.CanProcessSales);
                cmd.Parameters.AddWithValue("@Proc", perms.CanManageProcurement);
                cmd.Parameters.AddWithValue("@Rep", perms.CanViewReports);
                cmd.Parameters.AddWithValue("@Data", perms.CanManageData);

                try
                {
                    conn.Open();
                    bool success = cmd.ExecuteNonQuery() > 0;
                    if (success) LogActivity(currentUserId, $"Created new dynamic role: {roleName}", "User Management");
                    return success;
                }
                catch { return false; } // Catches duplicate role names
            }
        }

        // 3. Sequential UserID Generator
        public string GenerateNextUserID()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT TOP 1 UserID FROM [USER] WHERE UserID LIKE 'USR-%' ORDER BY UserID DESC", conn);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string lastId = result.ToString();
                        if (lastId.StartsWith("USR-") && int.TryParse(lastId.Substring(4), out int num))
                        {
                            return $"USR-{(num + 1):D3}"; // e.g., USR-005
                        }
                    }
                }
                catch { }
            }
            return "USR-001";
        }

        // 4. Fetch All Users for DataGrid
        public List<UserModel> GetAllUsers()
        {
            List<UserModel> users = new List<UserModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
                    SELECT UserID, FirstName, LastName, ContactNumber, Username, Role, Passkey, Status, 
                           CreatedBy, CreatedTime, ModifiedBy, LastModifiedTime 
                    FROM [USER] ORDER BY CreatedTime DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            UserID = reader["UserID"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            ContactNumber = reader["ContactNumber"]?.ToString(),
                            Username = reader["Username"].ToString(),
                            Role = reader["Role"].ToString(),
                            Passkey = reader["Passkey"]?.ToString(),
                            Status = reader["Status"].ToString(),
                            CreatedBy = reader["CreatedBy"]?.ToString(),
                            CreatedTime = reader["CreatedTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedTime"]) : null,
                            ModifiedBy = reader["ModifiedBy"]?.ToString(),
                            LastModifiedTime = reader["LastModifiedTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastModifiedTime"]) : null
                        });
                    }
                }
            }
            return users;
        }

        public string GeneratePasskey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            char[] passkey = new char[6];

            for (int i = 0; i < passkey.Length; i++)
            {
                passkey[i] = chars[random.Next(chars.Length)];
            }

            return new string(passkey);
        }

        // 5. Create a New User
        public bool CreateUser(UserModel user, string rawPassword, string currentUserId)
        {
            // 1. Automatically generate and assign the 6-character alphanumeric passkey
            user.Passkey = GeneratePasskey();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
            INSERT INTO [USER] (UserID, FirstName, LastName, ContactNumber, Username, PasswordHash, Role, Passkey, Status, CreatedBy, CreatedTime) 
            VALUES (@ID, @FN, @LN, @CN, @UN, @PH, @R, @PK, 'Active', @CB, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", user.UserID);
                cmd.Parameters.AddWithValue("@FN", user.FirstName);
                cmd.Parameters.AddWithValue("@LN", user.LastName);
                cmd.Parameters.AddWithValue("@CN", (object)user.ContactNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UN", user.Username);
                cmd.Parameters.AddWithValue("@PH", BCrypt.Net.BCrypt.HashPassword(rawPassword));
                cmd.Parameters.AddWithValue("@R", user.Role);

                // 2. Map the newly generated passkey to the SQL parameter
                cmd.Parameters.AddWithValue("@PK", user.Passkey);
                cmd.Parameters.AddWithValue("@CB", currentUserId);

                try
                {
                    conn.Open();
                    bool success = cmd.ExecuteNonQuery() > 0;
                    if (success) LogActivity(currentUserId, $"Created new user account: {user.Username}", "User Management");
                    return success;
                }
                catch { return false; }
            }
        }

        // 6. Update Existing User (No Password Change here)
        public bool UpdateUser(UserModel user, string currentUserId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
                    UPDATE [USER] 
                    SET FirstName=@FN, LastName=@LN, ContactNumber=@CN, Username=@UN, Role=@R, Passkey=@PK, 
                        ModifiedBy=@MB, LastModifiedTime=GETDATE() 
                    WHERE UserID=@ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", user.UserID);
                cmd.Parameters.AddWithValue("@FN", user.FirstName);
                cmd.Parameters.AddWithValue("@LN", user.LastName);
                cmd.Parameters.AddWithValue("@CN", (object)user.ContactNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UN", user.Username);
                cmd.Parameters.AddWithValue("@R", user.Role);
                cmd.Parameters.AddWithValue("@PK", user.Passkey);
                cmd.Parameters.AddWithValue("@MB", currentUserId);

                try
                {
                    conn.Open();
                    bool success = cmd.ExecuteNonQuery() > 0;
                    if (success) LogActivity(currentUserId, $"Updated user details for: {user.Username}", "User Management");
                    return success;
                }
                catch { return false; }
            }
        }

        // 7. Deactivate / Activate User (Soft Delete)
        public bool ToggleUserStatus(string targetUserId, string newStatus, string currentUserId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = "UPDATE [USER] SET Status=@Status, ModifiedBy=@MB, LastModifiedTime=GETDATE() WHERE UserID=@ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@MB", currentUserId);
                cmd.Parameters.AddWithValue("@ID", targetUserId);

                conn.Open();
                bool success = cmd.ExecuteNonQuery() > 0;
                if (success) LogActivity(currentUserId, $"Set user {targetUserId} status to {newStatus}", "User Management");
                return success;
            }
        }

        // 8. Fetch Permissions for a Specific Role
        public RolePermissions GetRolePermissions(string roleName)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = "SELECT CanManageUsers, CanManageInventory, CanProcessSales, CanManageProcurement, CanViewReports, CanManageData FROM [ROLE] WHERE RoleName = @Name";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", roleName);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new RolePermissions
                        {
                            CanManageUsers = Convert.ToBoolean(reader["CanManageUsers"]),
                            CanManageInventory = Convert.ToBoolean(reader["CanManageInventory"]),
                            CanProcessSales = Convert.ToBoolean(reader["CanProcessSales"]),
                            CanManageProcurement = Convert.ToBoolean(reader["CanManageProcurement"]),
                            CanViewReports = Convert.ToBoolean(reader["CanViewReports"]),
                            CanManageData = Convert.ToBoolean(reader["CanManageData"])
                        };
                    }
                }
            }
            return null;
        }

        // 9. Update an Existing Role
        public bool UpdateRole(string roleName, RolePermissions perms, string currentUserId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
                    UPDATE [ROLE] 
                    SET CanManageUsers=@Users, CanManageInventory=@Inv, CanProcessSales=@Sales, 
                        CanManageProcurement=@Proc, CanViewReports=@Rep, CanManageData=@Data
                    WHERE RoleName=@Name";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", roleName);
                cmd.Parameters.AddWithValue("@Users", perms.CanManageUsers);
                cmd.Parameters.AddWithValue("@Inv", perms.CanManageInventory);
                cmd.Parameters.AddWithValue("@Sales", perms.CanProcessSales);
                cmd.Parameters.AddWithValue("@Proc", perms.CanManageProcurement);
                cmd.Parameters.AddWithValue("@Rep", perms.CanViewReports);
                cmd.Parameters.AddWithValue("@Data", perms.CanManageData);

                try
                {
                    conn.Open();
                    bool success = cmd.ExecuteNonQuery() > 0;
                    if (success) LogActivity(currentUserId, $"Updated dynamic role permissions: {roleName}", "User Management");
                    return success;
                }
                catch { return false; }
            }
        }

        // 10. Reset User Passkey
        public string ResetUserPasskey(string targetUserId, string currentUserId)
        {
            // Generate a fresh passkey using our existing helper
            string newPasskey = GeneratePasskey();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = "UPDATE [USER] SET Passkey=@PK, ModifiedBy=@MB, LastModifiedTime=GETDATE() WHERE UserID=@ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PK", newPasskey);
                cmd.Parameters.AddWithValue("@MB", currentUserId);
                cmd.Parameters.AddWithValue("@ID", targetUserId);

                try
                {
                    conn.Open();
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        LogActivity(currentUserId, $"Reset passkey for user ID: {targetUserId}", "User Management");
                        return newPasskey;
                    }
                }
                catch { } // Fails silently to prevent crashes
            }
            return null; // Return null if the database update failed
        }
    }
}