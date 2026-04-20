using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Attendances
    {
        public long AttendanceID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime WorkDate { get; set; }
        public int? ShiftID { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public decimal? WorkingHours { get; set; }
        public int LateMinutes { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public DateTime? ConfirmedAt { get; set; }
        public int? ConfirmedBy { get; set; } 
        public int EarlyLeaveMinutes { get; set; }
        public bool IsHoliday { get; set; }
        public int? WorkTypeID { get; set; }
        public short? StatusID { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }

        [ForeignKey("WorkTypeId")]
        public virtual WorkType? WorkType { get; set; }

        [ForeignKey("StatusId")]
        public virtual StatusMasters? Status { get; set; }

        // Thêm navigation property này
        public virtual ICollection<AttendanceOvertime> AttendanceOvertimes { get; set; } = new List<AttendanceOvertime>();

    }

}
