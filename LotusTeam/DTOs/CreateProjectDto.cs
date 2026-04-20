namespace LotusTeam.DTOs
{
    public class CreateProjectDto
    {
        public string ProjectCode { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public short? StatusID { get; set; }
        public decimal? Budget { get; set; }
        public int? ManagerID { get; set; }
    }
}