using Microsoft.Data.SqlClient;
using System;

namespace SJ_PC_Store_SIMS.Utils
{
    public static class DatabaseHelper
    {
        // Change "localhost\\SQLEXPRESS" to your SQL server instance name if it's different
        private static readonly string connectionString = "Server=localhost\\SQLEXPRESS;Database=SJ_PC_STORE;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}