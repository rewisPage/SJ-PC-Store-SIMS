using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Utils;
using SJ_PC_Store_SIMS.Models;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class InventoryController
    {
        public void LogActivity(string userId, string action)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO ACTIVITY_LOG (UserID, ActionDescription) VALUES (@U, @A)", conn);
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@A", action);
                    conn.Open(); cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public string GenerateNextItemCode()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT TOP 1 ItemCode FROM ITEM_MASTER WHERE ItemCode LIKE 'ITM-%' ORDER BY ItemCode DESC", conn);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string lastCode = result.ToString();
                        if (lastCode.StartsWith("ITM-") && int.TryParse(lastCode.Substring(4), out int num))
                        {
                            return "ITM-" + (num + 1).ToString("D4");
                        }
                    }
                }
                catch { }
                return "ITM-1001";
            }
        }

        // MODIFIED: Fetches all audit history and calculates Sold/Defective totals
        private List<ItemMasterModel> FetchBlueprints(bool isActive)
        {
            List<ItemMasterModel> blueprints = new List<ItemMasterModel>();
            string query = @"
                SELECT 
                    i.ItemCode, i.Category, i.Specs, i.ItemCondition, i.BaselineCost, i.CurrentValue, i.IsActive,
                    i.CreatedTime, 
                    ISNULL(uc.FirstName + ' ' + uc.LastName, 'System') as CreatedByName,
                    i.LastModifiedTime, 
                    ISNULL(um.FirstName + ' ' + um.LastName, 'None') as ModifiedByName,
                    (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status = 'Available') as StockCount,
                    (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status = 'Sold') as SoldCount,
                    (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status = 'Defective') as DefectCount
                FROM ITEM_MASTER i
                LEFT JOIN [USER] uc ON i.CreatedBy = uc.UserID
                LEFT JOIN [USER] um ON i.ModifiedBy = um.UserID
                WHERE i.IsActive = @ActiveFlag";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ActiveFlag", isActive ? 1 : 0);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        blueprints.Add(new ItemMasterModel
                        {
                            ItemCode = reader["ItemCode"].ToString(),
                            Category = reader["Category"].ToString(),
                            Specs = reader["Specs"].ToString(),
                            ItemCondition = reader["ItemCondition"].ToString(),
                            BaselineCost = Convert.ToDecimal(reader["BaselineCost"]),
                            CurrentValue = Convert.ToDecimal(reader["CurrentValue"]),
                            PhysicalStockCount = Convert.ToInt32(reader["StockCount"]),
                            TotalSold = Convert.ToInt32(reader["SoldCount"]),
                            TotalDefective = Convert.ToInt32(reader["DefectCount"]),
                            CreatedTime = Convert.ToDateTime(reader["CreatedTime"]),
                            CreatedBy = reader["CreatedByName"].ToString(),
                            LastModifiedTime = reader["LastModifiedTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastModifiedTime"]) : (DateTime?)null,
                            ModifiedBy = reader["ModifiedByName"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return blueprints;
        }

        public List<ItemMasterModel> GetAllBlueprints() => FetchBlueprints(true);
        public List<ItemMasterModel> GetArchivedBlueprints() => FetchBlueprints(false);

        public bool CreateBlueprint(string code, string category, string specs, string condition, decimal baseCost, decimal currValue, string userId)
        {
            if (string.IsNullOrWhiteSpace(code)) code = GenerateNextItemCode();

            string query = "INSERT INTO ITEM_MASTER (ItemCode, Category, Specs, BaselineCost, CurrentValue, ItemCondition, IsActive, CreatedTime, CreatedBy) VALUES (@Code, @Cat, @Specs, @Base, @Curr, @Cond, 1, GETDATE(), @UID)";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code); cmd.Parameters.AddWithValue("@Cat", category); cmd.Parameters.AddWithValue("@Specs", specs); cmd.Parameters.AddWithValue("@Base", baseCost); cmd.Parameters.AddWithValue("@Curr", currValue); cmd.Parameters.AddWithValue("@Cond", condition); cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateBlueprint(string code, string category, string specs, string condition, decimal baseCost, decimal currValue, string userId)
        {
            string query = "UPDATE ITEM_MASTER SET Category=@Cat, Specs=@Specs, BaselineCost=@Base, CurrentValue=@Curr, ItemCondition=@Cond, LastModifiedTime=GETDATE(), ModifiedBy=@UID WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code); cmd.Parameters.AddWithValue("@Cat", category); cmd.Parameters.AddWithValue("@Specs", specs); cmd.Parameters.AddWithValue("@Base", baseCost); cmd.Parameters.AddWithValue("@Curr", currValue); cmd.Parameters.AddWithValue("@Cond", condition); cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteBlueprint(string itemCode, string userId)
        {
            string query = "UPDATE ITEM_MASTER SET IsActive = 0, LastModifiedTime=GETDATE(), ModifiedBy=@UID WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", itemCode); cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool RestoreBlueprint(string itemCode, string userId)
        {
            string query = "UPDATE ITEM_MASTER SET IsActive = 1, LastModifiedTime=GETDATE(), ModifiedBy=@UID WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", itemCode); cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public int GetBlueprintStockCount(string itemCode)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM STOCK_INSTANCE WHERE ItemCode=@Code AND Status='Available'", conn);
                cmd.Parameters.AddWithValue("@Code", itemCode); conn.Open(); return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<string> GetCategories()
        {
            List<string> categories = new List<string>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT CategoryName FROM CATEGORY_LIST", conn); conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader()) { while (reader.Read()) categories.Add(reader["CategoryName"].ToString()); }
                }
                catch { }
            }
            return categories;
        }

        public bool AddCategory(string categoryName)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO CATEGORY_LIST (CategoryName) VALUES (@Name)", conn);
                    cmd.Parameters.AddWithValue("@Name", categoryName); conn.Open(); return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch { return false; }
        }

        public bool DeleteCategory(string categoryName)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM CATEGORY_LIST WHERE CategoryName=@Name", conn);
                    cmd.Parameters.AddWithValue("@Name", categoryName); conn.Open(); return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch { return false; }
        }

        public DataTable GetPhysicalStock()
        {
            DataTable dt = new DataTable();
            string query = "SELECT SerialNumber, ItemCode, PO_Number, SupplierID, Status FROM STOCK_INSTANCE";
            using (SqlConnection conn = DatabaseHelper.GetConnection()) { try { SqlDataAdapter adapter = new SqlDataAdapter(query, conn); adapter.Fill(dt); } catch { } }
            return dt;
        }

        public bool FlagStockDefective(string serialNumber, string reason)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("UPDATE STOCK_INSTANCE SET Status='Defective', DefectReason=@Reason WHERE SerialNumber=@SN", conn);
                cmd.Parameters.AddWithValue("@SN", serialNumber); cmd.Parameters.AddWithValue("@Reason", reason); conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool VerifyUserPassword(string userId, string rawPassword)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT PasswordHash FROM [USER] WHERE UserID = @UID", conn);
                cmd.Parameters.AddWithValue("@UID", userId);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    // Uses BCrypt to verify the typed password against the DB hash
                    return BCrypt.Net.BCrypt.Verify(rawPassword, result.ToString());
                }
            }
            return false;
        }

        public int GetLifetimeStockCount(string itemCode)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Checks ALL statuses (Available, Sold, Defective)
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM STOCK_INSTANCE WHERE ItemCode = @Code", conn);
                cmd.Parameters.AddWithValue("@Code", itemCode);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool HardDeleteBlueprint(string itemCode, string userId)
        {
            LogActivity(userId, $"Permanently deleted blueprint: {itemCode}");
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM ITEM_MASTER WHERE ItemCode = @Code", conn);
                cmd.Parameters.AddWithValue("@Code", itemCode);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}