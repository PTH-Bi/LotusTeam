namespace LotusTeam.Models
{
    public class RemoteAttendances
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime WorkDate { get; set; }

        public string Reason { get; set; } = null!;

        public string Status { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
