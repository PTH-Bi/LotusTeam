namespace LotusTeam.Models
{
    public class Bonus
    {
        public int BonusID { get; set; }
        public int EmployeeID { get; set; }
        public int? PayrollID { get; set; } // Liên kết với bảng lương cụ thể
        public DateTime Month { get; set; }
        public string BonusName { get; set; } // Tên thưởng (VD: Thưởng doanh số, Thưởng chuyên cần)
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual Employees Employee { get; set; }
        public virtual Payrolls Payroll { get; set; }
    }
}
