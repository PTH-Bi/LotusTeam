namespace LotusTeam.DTOs
{
    public class ReviewCandidateDto
    {
        public int CandidateId { get; set; }
        public int EmployeeId { get; set; }

        public int? ReviewerId { get; set; }

        public decimal? Score { get; set; }

        public string? Comments { get; set; }

        public string? ReviewPeriod { get; set; }
    }
}