using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class LeaveBalances
    {
        [Key]
        public int LeaveBalanceID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [Required]
        public int Year { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        public decimal AnnualQuota { get; set; } = 12;

        [Column(TypeName = "decimal(4,1)")]
        public decimal UsedDays { get; set; } = 0;

        [Column(TypeName = "decimal(4,1)")]
        public decimal UnpaidDays { get; set; } = 0;

        public int ConsecutiveLeaveDays { get; set; } = 0;

        public DateTime? LastLeaveEndDate { get; set; }

        public bool IsReset { get; set; } = true;

        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("EmployeeID")]
        public Employees Employee { get; set; } = null!;
    }
}
