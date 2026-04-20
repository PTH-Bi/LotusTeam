using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("RolePermissions")]
    public class RolePermissions
    {
        [Key]
        public int RolePermissionID { get; set; }

        [Required]
        public int RoleID { get; set; }

        [Required]
        public int PermissionID { get; set; }

        /* Navigation properties */
        public Role Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
    }
}
