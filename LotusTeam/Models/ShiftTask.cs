using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class ShiftTask
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; } = string.Empty;

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        public short? StatusId { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("StatusId")]
        public virtual StatusMasters? Status { get; set; }
    }
}