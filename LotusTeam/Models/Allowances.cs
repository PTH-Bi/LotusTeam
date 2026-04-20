namespace LotusTeam.Models
{
    // Models/Allowance.cs - Thêm các trường mới
    public class Allowances
    {
        public int AllowanceID { get; set; }
        public int EmployeeID { get; set; }
        public int? PayrollID { get; set; }
        public DateTime Month { get; set; }
        public string AllowanceName { get; set; } // "Phụ cấp thân nhân"
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedDate { get; set; }

        // ===== THÊM CÁC TRƯỜNG CHO PHỤ CẤP THÂN NHÂN =====
        public bool IsDependentAllowance { get; set; } = false; // Đánh dấu là phụ cấp thân nhân
        public int? DependentCount { get; set; } // Số lượng người phụ thuộc
        public decimal? DependentAmountPerPerson { get; set; } // Mức phụ cấp mỗi người (VD: 500,000 VND/người)
                                                               // ===================================================

        // Navigation properties
        public virtual Employees Employee { get; set; }
        public virtual Payrolls Payroll { get; set; }
    }
}
