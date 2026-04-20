namespace LotusTeam.DTOs
{
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