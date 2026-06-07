namespace SJ_PC_Store_SIMS.Models
{
    public class ActivityLogModel
    {
        public int LogID { get; set; }
        public string UserID { get; set; }
        public string FullName { get; set; }
        public string ModuleCategory { get; set; }
        public string ActionDescription { get; set; }
        public DateTime LogDate { get; set; }
    }
}