using System.ComponentModel.DataAnnotations;

namespace LotusTeam.DTOs
{
    public class CreatePerformanceReviewDto
    {
        [Required]
        public int EmployeeID { get; set; }

        [Required]
        public DateTime ReviewDate { get; set; }

        public int? ReviewerId { get; set; }

        public decimal? Score { get; set; }

        public string? Comments { get; set; }

        public string? ReviewPeriod { get; set; }
    }
}