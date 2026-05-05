using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Requests
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestType { get; set; } = string.Empty; // LEAVE / OT / PROMOTION / REWARD

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public short StatusId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("StatusId")]
        public virtual StatusMasters Status { get; set; } = null!;

        [ForeignKey("ApprovedBy")]
        public virtual Employees? Approver { get; set; }

        // Reverse navigation
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public virtual ICollection<OvertimeRequests> OvertimeRequests { get; set; } = new List<OvertimeRequests>();
        public virtual ICollection<MealRequests> MealRequests { get; set; } = new List<MealRequests>();
        public virtual ICollection<SalaryPromotionRequests> SalaryPromotionRequests { get; set; } = new List<SalaryPromotionRequests>();
        public virtual ICollection<RewardsDisciplines> RewardsDisciplines { get; set; } = new List<RewardsDisciplines>();
    }
}