namespace LotusTeam.Models
{
    public class AttendanceOvertime
    {
        public long AttendanceOvertimeID { get; set; }
        public long AttendanceID { get; set; }
        public int RuleID { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal? CalculatedAmount { get; set; }

        public Attendances Attendance { get; set; } = null!;
        public OvertimeRule OvertimeRule { get; set; } = null!;
    }

}
