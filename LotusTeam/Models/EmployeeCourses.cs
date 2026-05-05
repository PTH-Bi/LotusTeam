using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class EmployeeCourses
    {
        [Key]
        public int EnrollmentId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateOnly EnrolledDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public DateOnly? CompletionDate { get; set; }

        public short StatusId { get; set; }

        [StringLength(500)]
        public string? CertificatePath { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("CourseId")]
        public virtual Courses Course { get; set; } = null!;

        [ForeignKey("StatusId")]
        public virtual StatusMasters? Status { get; set; }
    }
}