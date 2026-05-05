using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class AuditSoftDelete
    {
        [Key]
        public long AuditId { get; set; }

        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RecordId { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public DateTime? UpdatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation property
        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}