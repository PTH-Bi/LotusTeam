using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class CandidateCVs
    {
        [Key]
        public int CandidateCVID { get; set; }

        public int CandidateID { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public string CvText { get; set; }

        public int Score { get; set; }

        public bool IsSuitable { get; set; }

        public bool IsViewedByHR { get; set; }

        [StringLength(100)]
        public string? BestMatchedPosition { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        [ForeignKey("CandidateID")]
        public virtual Candidates Candidate { get; set; } = null!;
    }
}
