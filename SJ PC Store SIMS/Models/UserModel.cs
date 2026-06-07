namespace SJ_PC_Store_SIMS.Models
{
    public class UserModel
    {
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ContactNumber { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string Passkey { get; set; }

        // Audit Trail Properties
        public string CreatedBy { get; set; }
        public DateTime? CreatedTime { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? LastModifiedTime { get; set; }

        // Add the permissions object
        public RolePermissions Permissions { get; set; } = new RolePermissions();
    }

    public class RolePermissions
    {
        public bool CanManageUsers { get; set; }
        public bool CanManageInventory { get; set; }
        public bool CanProcessSales { get; set; }
        public bool CanManageProcurement { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanManageData { get; set; }
    }
}