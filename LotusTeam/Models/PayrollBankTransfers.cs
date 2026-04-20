using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("PayrollBankTransfers")]
    public class PayrollBankTransfers
    {
        [Key]
        public int TransferID { get; set; }

        [Required]
        public int PayrollID { get; set; }

        [Required]
        public int CompanyBankAccountID { get; set; }

        public DateTime? TransferDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "PENDING";

        [StringLength(100)]
        public string? BankBatchCode { get; set; }

        public DateTime CreatedDate { get; set; }

        /* Navigation */
        public Payrolls Payroll { get; set; } = null!;
        public CompanyBankAccounts CompanyBankAccount { get; set; } = null!;

        public ICollection<PayrollTransferDetails>? TransferDetails { get; set; }
    }
}