namespace LotusTeam.Models
{
    public class DependentAllowances
    {
        public int DependentAllowanceID { get; set; }
        public int EmployeeID { get; set; }
        public int? PayrollID { get; set; }
        public DateTime Month { get; set; }
        public int TotalDependents { get; set; } // Tổng số người phụ thuộc trong tháng
        public decimal AmountPerDependent { get; set; } // Mức phụ cấp mỗi người (VD: 500,000)
        public decimal TotalAmount { get; set; } // Tổng phụ cấp = TotalDependents * AmountPerDependent
        public string? Note { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation
        public virtual Employees Employee { get; set; }
        public virtual Payrolls Payroll { get; set; }
    }
}
