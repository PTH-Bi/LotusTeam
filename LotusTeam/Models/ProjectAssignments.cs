namespace LotusTeam.Models
{
    public class ProjectAssignment
    {
        public int AssignmentID { get; set; }
        public int EmployeeID { get; set; }
        public int ProjectID { get; set; }
        public string? Role { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string? ContributionDescription { get; set; }

        public Employees Employee { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }

}
