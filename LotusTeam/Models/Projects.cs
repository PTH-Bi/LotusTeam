namespace LotusTeam.Models
{
    public class Project
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

        public Employees? Manager { get; set; }
        public ICollection<ProjectAssignment>? ProjectAssignments { get; set; }
    }

}
