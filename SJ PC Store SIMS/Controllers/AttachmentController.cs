using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class AttachmentController
    {
        // Saves file to disk and inserts record into DB
        public bool UploadAttachment(string poNumber, string sourceFilePath, string uploadedBy, string transactionId = null)
        {
            string identifier = transactionId ?? poNumber;
            string fileName = Path.GetFileName(sourceFilePath);
            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Attachments", identifier);
            Directory.CreateDirectory(targetFolder);
            string targetPath = Path.Combine(targetFolder, fileName);

            if (File.Exists(targetPath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                fileName = $"{nameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                targetPath = Path.Combine(targetFolder, fileName);
            }

            File.Copy(sourceFilePath, targetPath, overwrite: true);

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO ATTACHMENTS (PO_Number, TransactionID, FileName, 
            FilePath, UploadedBy, UploadedDate) 
            VALUES (@PO_Number, @TransactionID, @FileName, @FilePath, @UploadedBy, GETDATE())";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PO_Number",
                        string.IsNullOrEmpty(poNumber) ? DBNull.Value : (object)poNumber);
                    cmd.Parameters.AddWithValue("@TransactionID",
                        string.IsNullOrEmpty(transactionId) ? DBNull.Value : (object)transactionId);
                    cmd.Parameters.AddWithValue("@FileName", fileName);
                    cmd.Parameters.AddWithValue("@FilePath", targetPath);
                    cmd.Parameters.AddWithValue("@UploadedBy", uploadedBy);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // Get all attachments for a PO
        public List<AttachmentModel> GetAttachments(string poNumber)
        {
            var list = new List<AttachmentModel>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM ATTACHMENTS WHERE PO_Number = @PO_Number ORDER BY UploadedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PO_Number", poNumber);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AttachmentModel
                            {
                                AttachmentID = Convert.ToInt32(reader["AttachmentID"]),
                                PO_Number = reader["PO_Number"].ToString(),
                                FileName = reader["FileName"].ToString(),
                                FilePath = reader["FilePath"].ToString(),
                                UploadedBy = reader["UploadedBy"].ToString(),
                                UploadedDate = Convert.ToDateTime(reader["UploadedDate"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // Delete an attachment (from disk and DB)
        public bool DeleteAttachment(int attachmentId)
        {
            string filePath = "";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Get file path first
                string selectQuery = "SELECT FilePath FROM ATTACHMENTS WHERE AttachmentID = @ID";
                using (var cmd = new SqlCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", attachmentId);
                    filePath = cmd.ExecuteScalar()?.ToString();
                }

                if (string.IsNullOrEmpty(filePath)) return false;

                // Delete from DB
                string deleteQuery = "DELETE FROM ATTACHMENTS WHERE AttachmentID = @ID";
                using (var cmd = new SqlCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", attachmentId);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0 && File.Exists(filePath))
                    {
                        try { File.Delete(filePath); } catch { }
                    }
                    return rows > 0;
                }
            }
        }

        public List<AttachmentModel> GetAttachmentsByTransaction(string transactionId)
        {
            var list = new List<AttachmentModel>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM ATTACHMENTS WHERE TransactionID = @TID ORDER BY UploadedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TID", transactionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AttachmentModel
                            {
                                AttachmentID = Convert.ToInt32(reader["AttachmentID"]),
                                PO_Number = reader["PO_Number"]?.ToString(),
                                FileName = reader["FileName"].ToString(),
                                FilePath = reader["FilePath"].ToString(),
                                UploadedBy = reader["UploadedBy"].ToString(),
                                UploadedDate = Convert.ToDateTime(reader["UploadedDate"])
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}