namespace LotusTeam.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }
        public string DepartmentCode { get; set; } = null!;
        public string DepartmentName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }

        public ICollection<Employees>? Employees { get; set; }
    }

}
