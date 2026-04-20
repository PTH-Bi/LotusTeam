using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("BankPartners")]
    public class BankPartner
    {
        [Key]
        public int BankPartnerID { get; set; }

        [Required]
        [StringLength(50)]
        public string BankCode { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string BankName { get; set; } = null!;

        [StringLength(100)]
        public string? ShortName { get; set; }

        [StringLength(50)]
        public string? SwiftCode { get; set; }

        [StringLength(500)]
        public string? ApiEndpoint { get; set; }

        [StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(50)]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        /* Navigation */
        public ICollection<Employees>? Employees { get; set; }
        public ICollection<CompanyBankAccounts>? CompanyBankAccounts { get; set; }
    }
}