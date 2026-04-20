using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("PayrollDetails")]
    public class PayrollDetails
    {
        [Key]
        public int PayrollDetailID { get; set; }

        [Required]
        public int PayrollID { get; set; }

        [Required]
        public int ComponentID { get; set; }

        [Required]
        [StringLength(100)]
        public string ComponentName { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsAddition { get; set; }

        [StringLength(50)]
        public string? SourceType { get; set; }

        public int? SourceID { get; set; }

        public decimal? Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Rate { get; set; }

        public DateTime? CreatedDate { get; set; }

        [StringLength(50)]
        public string? CreatedBy { get; set; }

        /* Navigation Properties */
        [ForeignKey("PayrollID")]
        public Payrolls Payroll { get; set; } = null!;

        [ForeignKey("ComponentID")]
        public PayrollComponents Component { get; set; } = null!;
    }
}