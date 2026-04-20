using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class PerformanceReview
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime ReviewDate { get; set; }

        public int? ReviewerId { get; set; }

        public decimal? Score { get; set; }

        public string? Comments { get; set; }

        [StringLength(50)]
        public string? ReviewPeriod { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("ReviewerId")]
        public virtual Employees? Reviewer { get; set; }
    }
}