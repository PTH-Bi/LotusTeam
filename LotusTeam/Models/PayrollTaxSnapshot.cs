using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("PayrollTaxSnapshot")]
    public class PayrollTaxSnapshot
    {
        [Key]
        public int SnapshotID { get; set; }   

        [Required]
        public int PayrollID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableIncome { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }
        [NotMapped]
        public decimal GrossIncome { get; set; }

        public DateTime CreatedDate { get; set; }

        // Navigation
        [ForeignKey("PayrollID")]
        public virtual Payrolls Payroll { get; set; } = null!;
    }
}