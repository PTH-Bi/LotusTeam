namespace LotusTeam.DTOs
{
    public class PerformanceReviewDto
    {
        public int ReviewId { get; set; }

        public int EmployeeID { get; set; }

        public DateTime ReviewDate { get; set; }

        public int? ReviewerId { get; set; }

        public decimal? Score { get; set; }

        public string? Comments { get; set; }

        public string? ReviewPeriod { get; set; }
    }
}