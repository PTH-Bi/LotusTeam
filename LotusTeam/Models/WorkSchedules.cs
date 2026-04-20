using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class WorkSchedules
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; } = null!;
    }
}