namespace LotusTeam.Models
{
    public class LeaveType
    {
        public int LeaveTypeID { get; set; }
        public string LeaveTypeCode { get; set; } = null!;
        public string LeaveTypeName { get; set; } = null!;
        public bool IsPaid { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

}
