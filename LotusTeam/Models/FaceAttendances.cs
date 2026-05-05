using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Models
{
    public class FaceAttendances
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public string? CapturedImage { get; set; }

        public double? Confidence { get; set; }

        public string Method { get; set; } = "FACE";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public Employees Employee { get; set; } = null!;
    }
}