namespace LotusTeam.Models
{
    public class LeaveRequest
    {
        public int LeaveID { get; set; }
        public int EmployeeID { get; set; }
        public int LeaveTypeID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal NumberOfDays { get; set; }
        public string? Reason { get; set; }
        public short? StatusID { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RequestID { get; set; }

        public Employees Employee { get; set; } = null!;
        public LeaveType LeaveType { get; set; } = null!;
    }

}
