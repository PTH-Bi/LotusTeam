using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Payrolls
    {
        public int PayrollID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime PayPeriod { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime? PaymentDate { get; set; }
        public short StatusID { get; set; }
        // Thêm các trường mới
        public decimal? AllowanceTotal { get; set; }  // Tổng phụ cấp
        public decimal? BonusTotal { get; set; }      // Tổng thưởng
        public decimal? OtherDeductions { get; set; } // Các khoản khấu trừ khác


        // Navigation properties
        //[ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        //[ForeignKey("StatusId")]
        public virtual StatusMasters Status { get; set; } = null!;

        // Thêm navigation property này
        public virtual ICollection<PayrollDetails> PayrollDetails { get; set; } = new List<PayrollDetails>();
        // Navigation properties
        public virtual ICollection<Allowances> Allowances { get; set; }
        public virtual ICollection<Bonus> Bonuses { get; set; }
        public virtual ICollection<Deductions> Deductions { get; set; }
        public virtual ICollection<DependentAllowances> DependentAllowances { get; set; }


    }
}
