using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class SalesController
    {
        public void LogActivity(string userId, string action)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO ACTIVITY_LOG (UserID, ActionDescription) VALUES (@U, @A)", conn);
                    cmd.Parameters.AddWithValue("@U", userId);
                    cmd.Parameters.AddWithValue("@A", action);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public string GenerateNextReceiptID()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    var cmd = new SqlCommand(
                        "SELECT TOP 1 ReceiptID FROM [TRANSACTION] ORDER BY CreatedOn DESC", conn);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string last = result.ToString();
                        if (last.StartsWith("RCPT-") && int.TryParse(last.Substring(5), out int num))
                            return $"RCPT-{num + 1:D4}";
                    }
                }
                catch { }
                return "RCPT-0001";
            }
        }

        public List<SalesTransactionModel> GetAllTransactions()
        {
            var list = new List<SalesTransactionModel>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                var cmd = new SqlCommand(
                    "SELECT * FROM [TRANSACTION] ORDER BY OrderDate DESC", conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SalesTransactionModel
                        {
                            ReceiptID = reader["ReceiptID"].ToString(),
                            CustomerName = reader["CustomerName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            TransactionNumber = reader["TransactionNumber"]?.ToString(),
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            SubTotal = reader["SubTotal"] != DBNull.Value ? Convert.ToDecimal(reader["SubTotal"]) : 0,
                            Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0,
                            Tax = reader["Tax"] != DBNull.Value ? Convert.ToDecimal(reader["Tax"]) : 0,
                            GrandTotal = Convert.ToDecimal(reader["GrandTotal"]),
                            Status = reader["Status"]?.ToString() ?? "Quotation",
                            WarrantyDays = reader["WarrantyDays"] != DBNull.Value ? Convert.ToInt32(reader["WarrantyDays"]) : 7,
                            CreatedBy = reader["CreatedBy"]?.ToString(),
                            CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),
                            ModifiedBy = reader["ModifiedBy"]?.ToString(),
                            ModifiedOn = reader["ModifiedOn"] != DBNull.Value ? Convert.ToDateTime(reader["ModifiedOn"]) : (DateTime?)null,
                            Remarks = reader["Remarks"]?.ToString()
                        });
                    }
                }
                // Load items for each transaction
                foreach (var txn in list)
                {
                    var iCmd = new SqlCommand(
                        @"SELECT ti.SerialNumber, ti.SoldPrice, s.ItemCode, i.Category, i.Specs, i.ItemCondition
                          FROM TRANSACTION_ITEM ti
                          JOIN STOCK_INSTANCE s ON ti.SerialNumber = s.SerialNumber
                          JOIN ITEM_MASTER i ON s.ItemCode = i.ItemCode
                          WHERE ti.ReceiptID = @R", conn);
                    iCmd.Parameters.AddWithValue("@R", txn.ReceiptID);
                    using (var iReader = iCmd.ExecuteReader())
                    {
                        while (iReader.Read())
                        {
                            txn.Items.Add(new SalesItemModel
                            {
                                SerialNumber = iReader["SerialNumber"].ToString(),
                                ItemCode = iReader["ItemCode"].ToString(),
                                Description = $"{iReader["Category"]} {iReader["Specs"]}",
                                ItemCondition = iReader["ItemCondition"].ToString(),
                                Quantity = 1,
                                UnitPrice = Convert.ToDecimal(iReader["SoldPrice"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public string SaveTransaction(SalesTransactionModel txn)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                try
                {
                    var cmd = new SqlCommand(
                        @"INSERT INTO [TRANSACTION] (ReceiptID, CustomerName, PaymentMethod, TransactionNumber,
                          OrderDate, SubTotal, Discount, Tax, GrandTotal, Status, WarrantyDays,
                          CreatedBy, CreatedOn, Remarks)
                          VALUES (@R, @C, @PM, @TN, @OD, @ST, @D, @T, @GT, @S, @WD, @CB, GETDATE(), @RM)", conn, transaction);
                    cmd.Parameters.AddWithValue("@R", txn.ReceiptID);
                    cmd.Parameters.AddWithValue("@C", (object)txn.CustomerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PM", txn.PaymentMethod ?? "Cash");
                    cmd.Parameters.AddWithValue("@TN", (object)txn.TransactionNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OD", txn.OrderDate);
                    cmd.Parameters.AddWithValue("@ST", txn.SubTotal);
                    cmd.Parameters.AddWithValue("@D", txn.Discount);
                    cmd.Parameters.AddWithValue("@T", txn.Tax);
                    cmd.Parameters.AddWithValue("@GT", txn.GrandTotal);
                    cmd.Parameters.AddWithValue("@S", txn.Status);
                    cmd.Parameters.AddWithValue("@WD", txn.WarrantyDays);
                    cmd.Parameters.AddWithValue("@CB", txn.CreatedBy);
                    cmd.Parameters.AddWithValue("@RM", (object)txn.Remarks ?? DBNull.Value);
                    cmd.ExecuteNonQuery();

                    foreach (var item in txn.Items)
                    {
                        var iCmd = new SqlCommand(
                            "INSERT INTO TRANSACTION_ITEM (ReceiptID, SerialNumber, SoldPrice) VALUES (@R, @SN, @SP)",
                            conn, transaction);
                        iCmd.Parameters.AddWithValue("@R", txn.ReceiptID);
                        iCmd.Parameters.AddWithValue("@SN", item.SerialNumber);
                        iCmd.Parameters.AddWithValue("@SP", item.UnitPrice);
                        iCmd.ExecuteNonQuery();
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

        public string UpdateTransaction(SalesTransactionModel txn)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                try
                {
                    var cmd = new SqlCommand(
                        @"UPDATE [TRANSACTION] SET CustomerName=@C, PaymentMethod=@PM, TransactionNumber=@TN,
                          OrderDate=@OD, SubTotal=@ST, Discount=@D, Tax=@T, GrandTotal=@GT,
                          Status=@S, WarrantyDays=@WD, Remarks=@RM, ModifiedBy=@MB, ModifiedOn=GETDATE()
                          WHERE ReceiptID=@R", conn, transaction);
                    cmd.Parameters.AddWithValue("@R", txn.ReceiptID);
                    cmd.Parameters.AddWithValue("@C", (object)txn.CustomerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PM", txn.PaymentMethod ?? "Cash");
                    cmd.Parameters.AddWithValue("@TN", (object)txn.TransactionNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OD", txn.OrderDate);
                    cmd.Parameters.AddWithValue("@ST", txn.SubTotal);
                    cmd.Parameters.AddWithValue("@D", txn.Discount);
                    cmd.Parameters.AddWithValue("@T", txn.Tax);
                    cmd.Parameters.AddWithValue("@GT", txn.GrandTotal);
                    cmd.Parameters.AddWithValue("@S", txn.Status);
                    cmd.Parameters.AddWithValue("@WD", txn.WarrantyDays);
                    cmd.Parameters.AddWithValue("@RM", (object)txn.Remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MB", txn.ModifiedBy ?? txn.CreatedBy);
                    cmd.ExecuteNonQuery();

                    // Remove old items
                    new SqlCommand("DELETE FROM TRANSACTION_ITEM WHERE ReceiptID=@R", conn, transaction)
                    { Parameters = { new SqlParameter("@R", txn.ReceiptID) } }.ExecuteNonQuery();

                    foreach (var item in txn.Items)
                    {
                        var iCmd = new SqlCommand(
                            "INSERT INTO TRANSACTION_ITEM (ReceiptID, SerialNumber, SoldPrice) VALUES (@R, @SN, @SP)",
                            conn, transaction);
                        iCmd.Parameters.AddWithValue("@R", txn.ReceiptID);
                        iCmd.Parameters.AddWithValue("@SN", item.SerialNumber);
                        iCmd.Parameters.AddWithValue("@SP", item.UnitPrice);
                        iCmd.ExecuteNonQuery();
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

        public bool UpdateTransactionStatus(string receiptID, string newStatus, string userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                var cmd = new SqlCommand(
                    "UPDATE [TRANSACTION] SET Status=@S, ModifiedBy=@U, ModifiedOn=GETDATE() WHERE ReceiptID=@R", conn);
                cmd.Parameters.AddWithValue("@S", newStatus);
                cmd.Parameters.AddWithValue("@U", userId);
                cmd.Parameters.AddWithValue("@R", receiptID);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ProcessPayment(string receiptID, string userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                try
                {
                    // Mark transaction as Paid
                    new SqlCommand(
                        "UPDATE [TRANSACTION] SET Status='Paid', ModifiedBy=@U, ModifiedOn=GETDATE() WHERE ReceiptID=@R",
                        conn, transaction)
                    { Parameters = { new SqlParameter("@U", userId), new SqlParameter("@R", receiptID) } }
                    .ExecuteNonQuery();

                    // Mark all items as Sold
                    new SqlCommand(
                        @"UPDATE STOCK_INSTANCE SET Status='Sold'
                          WHERE SerialNumber IN (SELECT SerialNumber FROM TRANSACTION_ITEM WHERE ReceiptID=@R)",
                        conn, transaction)
                    { Parameters = { new SqlParameter("@R", receiptID) } }
                    .ExecuteNonQuery();

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

        public bool ProcessReturn(string serialNumber, string defectReason, string userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                try
                {
                    new SqlCommand(
                        @"UPDATE STOCK_INSTANCE SET Status='Returned', DefectReason=@DR
                          WHERE SerialNumber=@SN", conn, transaction)
                    {
                        Parameters = { new SqlParameter("@SN", serialNumber), new SqlParameter("@DR", (object)defectReason ?? DBNull.Value) }
                    }.ExecuteNonQuery();

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

        public List<StockInstanceModel> GetAvailableStockForItem(string itemCode)
        {
            var list = new List<StockInstanceModel>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                var cmd = new SqlCommand(
                    "SELECT SerialNumber, ItemCode, Status FROM STOCK_INSTANCE WHERE ItemCode=@IC AND Status='Available'",
                    conn);
                cmd.Parameters.AddWithValue("@IC", itemCode);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(new StockInstanceModel { SerialNumber = reader["SerialNumber"].ToString(), ItemCode = reader["ItemCode"].ToString(), Status = reader["Status"].ToString() });
                }
            }
            return list;
        }
    }
}