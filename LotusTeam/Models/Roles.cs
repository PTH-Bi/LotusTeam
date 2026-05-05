namespace LotusTeam.Models
{
    public class Role
    {
        public int RoleID { get; set; }
        public string RoleCode { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }

        // Navigation properties
        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
        public virtual ICollection<RolePermissions> RolePermissions { get; set; } = new List<RolePermissions>(); // Thêm dòng này
    }
}

