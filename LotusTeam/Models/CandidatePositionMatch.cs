// Models/CandidatePositionMatch.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class CandidatePositionMatch
    {
        [Key]
        public int MatchId { get; set; }

        public int CandidateId { get; set; }

        public int JobPositionId { get; set; }

        public int? CandidateCVID { get; set; } // Liên kết với CV cụ thể

        public int TotalScore { get; set; }

        public bool IsSuitable { get; set; }

        public string? MatchedSkills { get; set; } // Lưu dạng JSON hoặc chuỗi cách nhau bởi dấu phẩy

        public DateTime MatchedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CandidateId")]
        public virtual Candidates Candidate { get; set; } = null!;

        [ForeignKey("JobPositionId")]
        public virtual JobPosition JobPosition { get; set; } = null!;

        [ForeignKey("CandidateCVID")]
        public virtual CandidateCVs? CandidateCV { get; set; }
    }
}