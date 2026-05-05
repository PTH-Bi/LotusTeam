namespace LotusTeam.DTOs
{
    public class CreateLeaveRequestDto
    {
        public int EmployeeID { get; set; }
        public int LeaveTypeID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal NumberOfDays { get; set; }
        public string? Reason { get; set; }
    }
}