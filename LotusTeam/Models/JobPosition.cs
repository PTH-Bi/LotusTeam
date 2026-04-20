// Models/JobPosition.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class JobPosition
    {
        [Key]
        public int JobPositionId { get; set; }

        [Required]
        [StringLength(100)]
        public string PositionName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int MinScoreRequired { get; set; } = 40;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public virtual ICollection<CandidatePositionMatch> CandidatePositionMatches { get; set; } = new List<CandidatePositionMatch>();
    }
}