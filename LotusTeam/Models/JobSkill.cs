// Models/JobSkill.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class JobSkill
    {
        [Key]
        public int JobSkillId { get; set; }

        public int JobPositionId { get; set; }

        [Required]
        [StringLength(100)]
        public string SkillName { get; set; } = string.Empty;

        public int Weight { get; set; } = 20; // Trọng số điểm cho kỹ năng này

        public bool IsRequired { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("JobPositionId")]
        public virtual JobPosition JobPosition { get; set; } = null!;
    }
}