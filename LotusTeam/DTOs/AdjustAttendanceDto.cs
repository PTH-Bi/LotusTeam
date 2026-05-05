namespace LotusTeam.DTOs
{
    public class AdjustAttendanceDto
    {
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}