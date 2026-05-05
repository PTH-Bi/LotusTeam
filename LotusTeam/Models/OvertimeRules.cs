namespace LotusTeam.Models
{
    public class OvertimeRule
    {
        public int RuleID { get; set; }
        public string RuleCode { get; set; } = null!;
        public string RuleName { get; set; } = null!;
        public decimal MinHours { get; set; }
        public decimal? MaxHours { get; set; }
        public decimal Rate { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsNight { get; set; }
        public bool IsActive { get; set; }

        public ICollection<AttendanceOvertime>? AttendanceOvertimes { get; set; }
    }

}
