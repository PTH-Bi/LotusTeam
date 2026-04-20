namespace LotusTeam.Models
{
    public class JobHistory
    {
        public int HistoryID { get; set; }
        public int EmployeeID { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ChangeReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now; // Thêm dòng này


        public Employees Employee { get; set; } = null!;
        public Department? Department { get; set; }
        public Position? Position { get; set; }
    }

}
