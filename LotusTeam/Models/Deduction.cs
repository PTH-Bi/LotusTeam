namespace LotusTeam.Models
{
    public class Deduction
    {
        public int DeductionID { get; set; }
        public int EmployeeID { get; set; }
        public int? PayrollID { get; set; }
        public DateTime Month { get; set; }
        public string DeductionName { get; set; } // Tên khấu trừ (VD: Tạm ứng, BHYT tự nguyện, Đoàn phí)
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual Employees Employee { get; set; }
        public virtual Payrolls Payroll { get; set; }
    }
}
