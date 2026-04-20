namespace LotusTeam.DTOs
{
    public class LeaveBalanceDto
    {
        public int EmployeeID { get; set; }
        public int Year { get; set; }

        public decimal AnnualQuota { get; set; }
        public decimal UsedDays { get; set; }
        public decimal UnpaidDays { get; set; }

        public int ConsecutiveLeaveDays { get; set; }

        public DateTime? LastLeaveEndDate { get; set; }

        public bool IsReset { get; set; }
    }
}