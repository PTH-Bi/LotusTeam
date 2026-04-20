namespace LotusTeam.DTOs
{
    public class CapabilityDto
    {
        public int EmployeeID { get; set; }
        public List<EmployeeSkillDto> Skills { get; set; } = new();
        public List<PerformanceReviewDto> RecentReviews { get; set; } = new();
    }
}