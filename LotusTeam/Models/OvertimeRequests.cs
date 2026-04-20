using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class OvertimeRequests
    {
        [Key]
        public int OvertimeRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateOnly WorkDate { get; set; }

        [Required]
        public TimeOnly FromTime { get; set; }

        [Required]
        public TimeOnly ToTime { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        public short StatusId { get; set; }

        public int? RequestId { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

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