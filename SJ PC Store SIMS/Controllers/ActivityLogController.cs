using Microsoft.Data.SqlClient;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;

namespace SJ_PC_Store_SIMS.Controllers
{
    public class ActivityLogController
    {
        public List<ActivityLogModel> GetFilteredLogs(DateTime fromDate, DateTime toDate, string category, string searchQuery)
        {
            List<ActivityLogModel> logs = new List<ActivityLogModel>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                // We use LEFT JOIN to gracefully handle scenarios where an activity exists but the User record is missing
                string query = @"
                    SELECT a.LogID, a.UserID, ISNULL(u.FirstName + ' ' + u.LastName, 'Unknown User') AS FullName, 
                           a.ModuleCategory, a.ActionDescription, a.LogDate
                    FROM ACTIVITY_LOG a
                    LEFT JOIN [USER] u ON a.UserID = u.UserID
                    WHERE a.LogDate >= @FromDate AND a.LogDate < @ToDate
                ";

                // 1. Module Category Filter
                if (category != "All Modules" && !string.IsNullOrEmpty(category))
                {
                    query += " AND a.ModuleCategory = @Category ";
                }

                // 2. Search Box Filter
                if (!string.IsNullOrEmpty(searchQuery) && searchQuery != "Search Logs...")
                {
                    query += " AND (a.ActionDescription LIKE @Search OR a.UserID LIKE @Search OR (u.FirstName + ' ' + u.LastName) LIKE @Search) ";
                }

                query += " ORDER BY a.LogDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);

                // Set the date range. ToDate adds 1 day to ensure it captures logs until 23:59:59 of the selected end date
                cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
                cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1));

                if (category != "All Modules" && !string.IsNullOrEmpty(category))
                    cmd.Parameters.AddWithValue("@Category", category);

                if (!string.IsNullOrEmpty(searchQuery) && searchQuery != "Search Logs...")
                    cmd.Parameters.AddWithValue("@Search", "%" + searchQuery + "%");

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new ActivityLogModel
                            {
                                LogID = Convert.ToInt32(reader["LogID"]),
                                UserID = reader["UserID"].ToString(),
                                FullName = reader["FullName"].ToString(),
                                ModuleCategory = reader["ModuleCategory"]?.ToString() ?? "Uncategorized",
                                ActionDescription = reader["ActionDescription"].ToString(),
                                LogDate = Convert.ToDateTime(reader["LogDate"])
                            });
                        }
                    }
                }
                catch { /* Fails silently to prevent UI crashes if DB locks up */ }
            }
            return logs;
        }
    }
}