using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("CompanyBankAccounts")]
    public class CompanyBankAccounts
    {
        [Key]
        public int CompanyBankAccountID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int BankPartnerID { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string AccountName { get; set; } = null!;

        [StringLength(200)]
        public string? Branch { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        /* Navigation */
        public CompanyInfo Company { get; set; } = null!;
        public BankPartner BankPartner { get; set; } = null!;
    }
}