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
        // Fetches data for TAB 1: Blueprint Catalog
        public List<ItemMasterModel> GetAllBlueprints()
        {
            List<ItemMasterModel> blueprints = new List<ItemMasterModel>();
            string query = @"
                SELECT 
                    i.ItemCode, i.Category, i.Specs, i.ItemCondition, i.BaselineCost, i.CurrentValue,
                    (SELECT COUNT(*) FROM STOCK_INSTANCE s WHERE s.ItemCode = i.ItemCode AND s.Status = 'Available') as StockCount
                FROM ITEM_MASTER i";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
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
                        PhysicalStockCount = Convert.ToInt32(reader["StockCount"])
                    });
                }
            }
            return blueprints;
        }

        // Fetches data for TAB 2: Physical Stock
        public DataTable GetPhysicalStock()
        {
            DataTable dt = new DataTable();
            string query = "SELECT SerialNumber, ItemCode, PO_Number, SupplierID, Status FROM STOCK_INSTANCE";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                adapter.Fill(dt);
            }
            return dt;
        }
    }
}