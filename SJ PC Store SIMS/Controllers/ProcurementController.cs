using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class ProcurementController
    {
        public void LogActivity(string userId, string action)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO ACTIVITY_LOG (UserID, ActionDescription) VALUES (@U, @A)", conn);
                    cmd.Parameters.AddWithValue("@U", userId); cmd.Parameters.AddWithValue("@A", action);
                    conn.Open(); cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public string GenerateNextPONumber()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT TOP 1 PO_Number FROM PROCUREMENT ORDER BY CreatedOn DESC", conn);
                    conn.Open(); object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string last = result.ToString();
                        if (last.StartsWith("PO-") && int.TryParse(last.Split('-')[2], out int num))
                            return $"PO-{DateTime.Now.Year}-{(num + 1):D3}";
                    }
                }
                catch { }
                return $"PO-{DateTime.Now.Year}-001";
            }
        }

        public List<ProcurementModel> GetAllProcurements()
        {
            List<ProcurementModel> list = new List<ProcurementModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT p.*, s.CompanyName, s.ContactPerson, s.ContactNumber FROM PROCUREMENT p JOIN SUPPLIER s ON p.SupplierID = s.SupplierID ORDER BY p.OrderDate DESC", conn);
                conn.Open();
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var po = new ProcurementModel
                            {
                                PO_Number = reader["PO_Number"].ToString(),
                                SupplierID = reader["SupplierID"].ToString(),
                                SupplierName = reader["CompanyName"].ToString(),
                                ContactPerson = reader["ContactPerson"]?.ToString(),
                                ContactNumber = reader["ContactNumber"]?.ToString(),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                ExpectedDate = reader["ExpectedDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExpectedDate"]) : DateTime.Now,
                                Status = reader["Status"].ToString(),
                                Remarks = reader["Remarks"]?.ToString(),
                                SubTotal = reader["SubTotal"] != DBNull.Value ? Convert.ToDecimal(reader["SubTotal"]) : 0,
                                Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0,
                                Tax = reader["Tax"] != DBNull.Value ? Convert.ToDecimal(reader["Tax"]) : 0, // Added Tax retrieval
                                GrandTotal = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0,
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                ApprovedBy = reader["ModifiedBy"]?.ToString()
                            };
                            list.Add(po);
                        }
                    }

                    // Fetch items for each PO, JOINING WITH ITEM_MASTER to get Category and Specs
                    foreach (var po in list)
                    {
                        SqlCommand iCmd = new SqlCommand("SELECT pi.ItemCode, pi.Quantity, pi.UnitPrice, i.Category, i.Specs, i.ItemCondition FROM PROCUREMENT_ITEM pi JOIN ITEM_MASTER i ON pi.ItemCode = i.ItemCode WHERE pi.PO_Number = @PO", conn);
                        iCmd.Parameters.AddWithValue("@PO", po.PO_Number);
                        using (SqlDataReader iReader = iCmd.ExecuteReader())
                        {
                            while (iReader.Read())
                            {
                                po.Items.Add(new ProcurementItemModel
                                {
                                    ItemCode = iReader["ItemCode"].ToString(),
                                    // Combine Category and Specs for the Description
                                    Description = $"{iReader["Category"]} {iReader["Specs"]}",
                                    Quantity = Convert.ToInt32(iReader["Quantity"]),
                                    UnitPrice = Convert.ToDecimal(iReader["UnitPrice"])
                                });
                            }
                        }
                    }
                }
                catch { }
            }
            return list;
        }

        public bool UpdatePOStatus(string poNumber, string newStatus, string userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("UPDATE PROCUREMENT SET Status = @S, ModifiedBy = @U, ModifiedOn = GETDATE() WHERE PO_Number = @PO", conn);
                cmd.Parameters.AddWithValue("@S", newStatus); cmd.Parameters.AddWithValue("@U", userId); cmd.Parameters.AddWithValue("@PO", poNumber);
                conn.Open(); return cmd.ExecuteNonQuery() > 0;
            }
        }

        // PHASE 3 & 4 WORKFLOW: Atomic Goods Receipt & Inventory Injection
        public bool ProcessGoodsReceipt(string poNumber, List<StockInstanceModel> physicalItems, string userId)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // 1. Insert physical serial numbers into Inventory
                    foreach (var item in physicalItems)
                    {
                        SqlCommand cmd = new SqlCommand("INSERT INTO STOCK_INSTANCE (SerialNumber, ItemCode, Status, PO_Number) VALUES (@SN, @IC, @S, @PO)", conn, transaction);
                        cmd.Parameters.AddWithValue("@SN", item.SerialNumber); cmd.Parameters.AddWithValue("@IC", item.ItemCode);
                        cmd.Parameters.AddWithValue("@S", item.Status); cmd.Parameters.AddWithValue("@PO", poNumber);
                        cmd.ExecuteNonQuery();
                    }
                    // 2. Mark PO as Completed
                    SqlCommand statCmd = new SqlCommand("UPDATE PROCUREMENT SET Status = 'Completed', ModifiedBy = @U, ModifiedOn = GETDATE() WHERE PO_Number = @PO", conn, transaction);
                    statCmd.Parameters.AddWithValue("@U", userId); statCmd.Parameters.AddWithValue("@PO", poNumber);
                    statCmd.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public string SavePO(ProcurementModel po)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // Validate that SupplierID exists before inserting
                    SqlCommand validateCmd = new SqlCommand("SELECT COUNT(*) FROM SUPPLIER WHERE SupplierID = @Sup", conn, transaction);
                    validateCmd.Parameters.AddWithValue("@Sup", po.SupplierID);
                    int supplierExists = (int)validateCmd.ExecuteScalar();

                    if (supplierExists == 0)
                    {
                        return $"ERROR: Supplier ID '{po.SupplierID}' does not exist in the database.";
                    }

                    string headerQuery = "INSERT INTO PROCUREMENT (PO_Number, SupplierID, OrderDate, ExpectedDate, Status, CreatedBy, CreatedOn, Remarks, SubTotal, Discount, Tax, GrandTotal) VALUES (@PO, @Sup, @OD, @ED, @Stat, @CB, GETDATE(), @Rem, @Sub, @Disc, @Tax, @Grand)";

                    SqlCommand cmd = new SqlCommand(headerQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@PO", po.PO_Number);
                    cmd.Parameters.AddWithValue("@Sup", po.SupplierID);
                    cmd.Parameters.AddWithValue("@OD", po.OrderDate);
                    cmd.Parameters.AddWithValue("@ED", po.ExpectedDate);
                    cmd.Parameters.AddWithValue("@Stat", po.Status);
                    cmd.Parameters.AddWithValue("@CB", po.CreatedBy); // MUST MATCH A VALID UserID IN YOUR DB
                    cmd.Parameters.AddWithValue("@Rem", po.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@Sub", po.SubTotal);
                    cmd.Parameters.AddWithValue("@Disc", po.Discount);
                    cmd.Parameters.AddWithValue("@Tax", po.Tax);
                    cmd.Parameters.AddWithValue("@Grand", po.GrandTotal);
                    cmd.ExecuteNonQuery();

                    foreach (var item in po.Items)
                    {
                        SqlCommand itemCmd = new SqlCommand("INSERT INTO PROCUREMENT_ITEM (PO_Number, ItemCode, Quantity, UnitPrice) VALUES (@PO, @IC, @Qty, @Price)", conn, transaction);
                        itemCmd.Parameters.AddWithValue("@PO", po.PO_Number);
                        itemCmd.Parameters.AddWithValue("@IC", item.ItemCode);
                        itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@Price", item.UnitPrice);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return "SUCCESS";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ex.Message; // Exposes the exact database error!
                }
            }
        }

        public string UpdatePO(ProcurementModel po)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // 1. Safe Fallback for User ID to prevent ModifiedBy Foreign Key crashes
                    string validUser = po.CreatedBy;
                    SqlCommand chkUser = new SqlCommand("SELECT COUNT(*) FROM [USER] WHERE UserID = @U", conn, transaction);
                    chkUser.Parameters.AddWithValue("@U", validUser ?? "");
                    if ((int)chkUser.ExecuteScalar() == 0)
                    {
                        SqlCommand getU = new SqlCommand("SELECT TOP 1 UserID FROM [USER]", conn, transaction);
                        object res = getU.ExecuteScalar();
                        if (res != null) validUser = res.ToString();
                    }

                    // 2. Validate Supplier exists
                    SqlCommand valSup = new SqlCommand("SELECT COUNT(*) FROM SUPPLIER WHERE SupplierID = @Sup", conn, transaction);
                    valSup.Parameters.AddWithValue("@Sup", po.SupplierID);
                    if ((int)valSup.ExecuteScalar() == 0)
                    {
                        return $"ERROR: Supplier ID '{po.SupplierID}' does not exist in the database.";
                    }

                    // 3. Update the Header Information
                    string headerQuery = "UPDATE PROCUREMENT SET SupplierID = @Sup, OrderDate = @OD, ExpectedDate = @ED, Status = @Stat, ModifiedBy = @MB, ModifiedOn = GETDATE(), Remarks = @Rem, SubTotal = @Sub, Discount = @Disc, Tax = @Tax, GrandTotal = @Grand WHERE PO_Number = @PO";

                    SqlCommand cmd = new SqlCommand(headerQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@PO", po.PO_Number);
                    cmd.Parameters.AddWithValue("@Sup", po.SupplierID);
                    cmd.Parameters.AddWithValue("@OD", po.OrderDate);
                    cmd.Parameters.AddWithValue("@ED", po.ExpectedDate);
                    cmd.Parameters.AddWithValue("@Stat", po.Status);
                    cmd.Parameters.AddWithValue("@MB", validUser);
                    cmd.Parameters.AddWithValue("@Rem", po.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@Sub", po.SubTotal);
                    cmd.Parameters.AddWithValue("@Disc", po.Discount);
                    cmd.Parameters.AddWithValue("@Tax", po.Tax);
                    cmd.Parameters.AddWithValue("@Grand", po.GrandTotal);
                    cmd.ExecuteNonQuery();

                    // 4. Delete existing items for this PO (Safest way to handle row edits/deletions)
                    SqlCommand delCmd = new SqlCommand("DELETE FROM PROCUREMENT_ITEM WHERE PO_Number = @PO", conn, transaction);
                    delCmd.Parameters.AddWithValue("@PO", po.PO_Number);
                    delCmd.ExecuteNonQuery();

                    // 5. Insert the newly edited items
                    foreach (var item in po.Items)
                    {
                        SqlCommand itemCmd = new SqlCommand("INSERT INTO PROCUREMENT_ITEM (PO_Number, ItemCode, Quantity, UnitPrice) VALUES (@PO, @IC, @Qty, @Price)", conn, transaction);
                        itemCmd.Parameters.AddWithValue("@PO", po.PO_Number);
                        itemCmd.Parameters.AddWithValue("@IC", item.ItemCode);
                        itemCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@Price", item.UnitPrice);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return "SUCCESS";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ex.Message;
                }
            }
        }
    }
}