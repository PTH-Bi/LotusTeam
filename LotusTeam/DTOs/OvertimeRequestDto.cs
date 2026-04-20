namespace LotusTeam.DTOs
{
    public class OvertimeRequestDto
    {
        public long AttendanceID { get; set; }
        public int RuleID { get; set; }
        public decimal OvertimeHours { get; set; }
    }
}