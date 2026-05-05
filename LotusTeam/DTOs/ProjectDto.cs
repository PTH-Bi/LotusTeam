namespace LotusTeam.DTOs
{
    public class ProjectDto
    {
        public int ProjectID { get; set; }
        public string ProjectCode { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public short? StatusID { get; set; }
        public decimal? Budget { get; set; }
        public int? ManagerID { get; set; }
        public string? ManagerName { get; set; }
    }

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

    public class AssignProjectDto
    {
        public int EmployeeID { get; set; }
        public int ProjectID { get; set; }
        public string? Role { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string? ContributionDescription { get; set; }
    }
}