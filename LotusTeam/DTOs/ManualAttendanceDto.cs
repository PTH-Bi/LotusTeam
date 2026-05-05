namespace LotusTeam.DTOs
{
    public class ManualAttendanceDto
    {
        public int EmployeeID { get; set; }
        public DateTime WorkDate { get; set; }
        public int? ShiftID { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public string? Notes { get; set; }
    }
}