using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class ContractTemplates
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(50)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        public int? ContractTypeId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? FilePath { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ContractTypeId")]
        public virtual ContractType? ContractType { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}