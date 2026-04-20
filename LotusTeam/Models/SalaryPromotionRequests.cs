using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class SalaryPromotionRequests
    {
        [Key]
        public int PromotionRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestType { get; set; } = string.Empty; // SALARY_INCREASE / PROMOTION

        [StringLength(200)]
        public string? CurrentValue { get; set; }

        [Required]
        [StringLength(200)]
        public string ExpectedValue { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required]
        public short StatusId { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? RequestId { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("ApprovedBy")]
        public virtual Employees? Approver { get; set; }

        [ForeignKey("StatusId")]
        public virtual StatusMasters Status { get; set; } = null!;

        [ForeignKey("RequestId")]
        public virtual Requests? Request { get; set; }
    }
}