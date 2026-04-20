namespace LotusTeam.DTOs
{
    public class EmployeeSimpleDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
