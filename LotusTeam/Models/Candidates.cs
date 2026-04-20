using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Candidates
    {
        [Key]
        public int CandidateId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? ResumePath { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ResumeContent { get; set; }

        [Required]
        [StringLength(100)]
        public string AppliedPosition { get; set; } = string.Empty;

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        public short? StatusId { get; set; }

        public string? Notes { get; set; }

        // Navigation property
        [ForeignKey("StatusId")]
        public virtual StatusMasters? Status { get; set; }
    }
}