using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Models
{
    public class CompanyInfo
    {
        [Key]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(50)]
        public string CompanyCode { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = null!;

        [MaxLength(50)]
        public string? TaxCode { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // Bank info
        [MaxLength(50)]
        public string? BankAccount { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? BankBranch { get; set; }

        [MaxLength(100)]
        public string? Representative { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }
    }
}
