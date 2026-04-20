using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Models
{
    public class User
    {
        public int UserID { get; set; }

        public int? EmployeeID { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedDate { get; set; }

        // ====== Thêm thông tin ngân hàng ======
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? BankBranch { get; set; }

        // ===== Navigation =====
        public Employees? Employee { get; set; }

        public virtual ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}
