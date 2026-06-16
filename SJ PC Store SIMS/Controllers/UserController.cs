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

        public string CreateRole(string roleName, RolePermissions perms, string currentUserId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // 1. Explicit validation: Prevent Duplicate Primary Keys
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM [ROLE] WHERE RoleName = @Name", conn);
                checkCmd.Parameters.AddWithValue("@Name", roleName);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    return $"ERROR: The role '{roleName}' already exists. Please choose a different name.";
                }

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
                    cmd.ExecuteNonQuery();
                    LogActivity(currentUserId, $"Created new dynamic role: {roleName}", "User Management");
                    return "SUCCESS";
                }
                catch (Exception ex)
                {
                    return $"Database error: {ex.Message}";
                }
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
                            return $"USR-{(num + 1):D3}";
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
        public string CreateUser(UserModel user, string rawPassword, string currentUserId)
        {
            user.Passkey = GeneratePasskey();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // 1. Explicit validation: Prevent Duplicate Usernames
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM [USER] WHERE Username = @UN", conn);
                checkCmd.Parameters.AddWithValue("@UN", user.Username);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    return $"ERROR: The username '{user.Username}' is already taken.";
                }

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
                cmd.Parameters.AddWithValue("@PK", user.Passkey);
                cmd.Parameters.AddWithValue("@CB", currentUserId);

                try
                {
                    cmd.ExecuteNonQuery();
                    LogActivity(currentUserId, $"Created new user account: {user.Username}", "User Management");
                    return "SUCCESS";
                }
                catch (Exception ex)
                {
                    return $"Database error: {ex.Message}";
                }
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

        // 10. Reset User Passkey (Now Accepts Dynamic Module Category)
        public string ResetUserPasskey(string targetUserId, string currentUserId, string moduleCategory = "User Management")
        {
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
                        // Dynamic module logging
                        LogActivity(currentUserId, $"Reset recovery passkey for user ID: {targetUserId}", moduleCategory);
                        return newPasskey;
                    }
                }
                catch { }
            }
            return null;
        }

        // 11. Fetch Specific User for Profile View
        public UserModel GetUserById(string userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                string query = @"
                    SELECT UserID, FirstName, LastName, ContactNumber, Username, Role, Passkey, Status, 
                           CreatedBy, CreatedTime, ModifiedBy, LastModifiedTime 
                    FROM [USER] WHERE UserID = @ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserModel
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
                        };
                    }
                }
            }
            return null;
        }

        // 12. Update Password from Profile View
        public bool ChangeUserPassword(string userId, string currentPassword, string newPassword)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string getHashQuery = "SELECT PasswordHash FROM [USER] WHERE UserID = @ID";
                string storedHash = "";

                using (SqlCommand cmd = new SqlCommand(getHashQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", userId);
                    var result = cmd.ExecuteScalar();
                    if (result != null) storedHash = result.ToString();
                }

                if (!string.IsNullOrEmpty(storedHash) && BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
                {
                    string updateQuery = "UPDATE [USER] SET PasswordHash = @PH, ModifiedBy = @MB, LastModifiedTime = GETDATE() WHERE UserID = @ID";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PH", BCrypt.Net.BCrypt.HashPassword(newPassword));
                        cmd.Parameters.AddWithValue("@MB", userId);
                        cmd.Parameters.AddWithValue("@ID", userId);
                        bool success = cmd.ExecuteNonQuery() > 0;

                        // Force logging strictly into the "Profile" Category
                        if (success) LogActivity(userId, "Updated account login password.", "Profile");
                        return success;
                    }
                }
                return false;
            }
        }
    }
}