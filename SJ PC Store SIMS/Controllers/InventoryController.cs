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
        // --- SYSTEM LOGGING ---
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

        // --- SMART ITEM CODE GENERATOR ---
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

        // ==========================================
        // ITEM MASTER (BLUEPRINT) CRUD
        // ==========================================
        public List<ItemMasterModel> GetAllBlueprints()
        {
            List<ItemMasterModel> blueprints = new List<ItemMasterModel>();
            string query = @"SELECT i.ItemCode, i.Category, i.Specs, i.ItemCondition, i.BaselineCost, i.CurrentValue,
                             (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status = 'Available') as StockCount
                             FROM ITEM_MASTER i WHERE i.IsActive = 1";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn); conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        blueprints.Add(new ItemMasterModel { ItemCode = reader["ItemCode"].ToString(), Category = reader["Category"].ToString(), Specs = reader["Specs"].ToString(), ItemCondition = reader["ItemCondition"].ToString(), BaselineCost = Convert.ToDecimal(reader["BaselineCost"]), CurrentValue = Convert.ToDecimal(reader["CurrentValue"]), PhysicalStockCount = Convert.ToInt32(reader["StockCount"]) });
                    }
                }
            }
            return blueprints;
        }

        public List<ItemMasterModel> GetArchivedBlueprints()
        {
            List<ItemMasterModel> blueprints = new List<ItemMasterModel>();
            string query = @"SELECT i.ItemCode, i.Category, i.Specs, i.ItemCondition, i.BaselineCost, i.CurrentValue
                             FROM ITEM_MASTER i WHERE i.IsActive = 0";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn); conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        blueprints.Add(new ItemMasterModel { ItemCode = reader["ItemCode"].ToString(), Category = reader["Category"].ToString(), Specs = reader["Specs"].ToString(), ItemCondition = reader["ItemCondition"].ToString(), BaselineCost = Convert.ToDecimal(reader["BaselineCost"]), CurrentValue = Convert.ToDecimal(reader["CurrentValue"]), PhysicalStockCount = 0 });
                    }
                }
            }
            return blueprints;
        }

        public bool CreateBlueprint(string code, string category, string specs, string condition, decimal baseCost, decimal currValue)
        {
            // FIX: Absolute safety net! If the view tries to send an empty code, it generates one automatically here.
            if (string.IsNullOrWhiteSpace(code)) code = GenerateNextItemCode();

            string query = "INSERT INTO ITEM_MASTER (ItemCode, Category, Specs, BaselineCost, CurrentValue, ItemCondition, IsActive) VALUES (@Code, @Cat, @Specs, @Base, @Curr, @Cond, 1)";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code); cmd.Parameters.AddWithValue("@Cat", category); cmd.Parameters.AddWithValue("@Specs", specs); cmd.Parameters.AddWithValue("@Base", baseCost); cmd.Parameters.AddWithValue("@Curr", currValue); cmd.Parameters.AddWithValue("@Cond", condition);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateBlueprint(string code, string category, string specs, string condition, decimal baseCost, decimal currValue)
        {
            string query = "UPDATE ITEM_MASTER SET Category=@Cat, Specs=@Specs, BaselineCost=@Base, CurrentValue=@Curr, ItemCondition=@Cond WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", code); cmd.Parameters.AddWithValue("@Cat", category); cmd.Parameters.AddWithValue("@Specs", specs); cmd.Parameters.AddWithValue("@Base", baseCost); cmd.Parameters.AddWithValue("@Curr", currValue); cmd.Parameters.AddWithValue("@Cond", condition);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteBlueprint(string itemCode)
        {
            string query = "UPDATE ITEM_MASTER SET IsActive = 0 WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", itemCode); conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool RestoreBlueprint(string itemCode)
        {
            string query = "UPDATE ITEM_MASTER SET IsActive = 1 WHERE ItemCode=@Code";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", itemCode); conn.Open(); return cmd.ExecuteNonQuery() > 0;
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
    }
}