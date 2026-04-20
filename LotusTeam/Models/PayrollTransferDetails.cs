using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("PayrollTransferDetails")]
    public class PayrollTransferDetails
    {
        [Key]
        public int TransferDetailID { get; set; }

        [Required]
        public int TransferID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [Required]
        [StringLength(50)]
        public string BankAccount { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string BankName { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string AccountName { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "PENDING";

        /* Navigation */
        public PayrollBankTransfers PayrollBankTransfer { get; set; } = null!;
        public Employees Employee { get; set; } = null!;
    }
}