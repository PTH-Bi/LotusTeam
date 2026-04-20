namespace LotusTeam.Models
{
    public class Permission
    {
        public int PermissionID { get; set; }
        public string PermissionCode { get; set; } = null!;
        public string PermissionName { get; set; } = null!;
        public string Module { get; set; } = null!;
    }

}
