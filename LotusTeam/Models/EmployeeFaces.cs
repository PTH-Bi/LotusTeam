namespace LotusTeam.Models
{
    public class EmployeeFaces
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string? ImagePath { get; set; }

        public string FaceEncoding { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public Employees Employee { get; set; } = null!;
    }
}
