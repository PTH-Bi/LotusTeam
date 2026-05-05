namespace LotusTeam.Models
{
    public class EmployeeAsset
    {
        public int AssignmentID { get; set; }
        public int EmployeeID { get; set; }
        public int AssetID { get; set; }
        public DateTime AssignDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? Notes { get; set; }

        public Employees Employee { get; set; } = null!;
        public Asset Asset { get; set; } = null!;
    }

}
