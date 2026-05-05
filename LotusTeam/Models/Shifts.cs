namespace LotusTeam.Models
{
    public class Shift
    {
        public int ShiftID { get; set; }
        public string ShiftCode { get; set; } = null!;
        public string ShiftName { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int BreakMinutes { get; set; }
        public bool IsNightShift { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Attendances>? Attendances { get; set; }
    }

}
