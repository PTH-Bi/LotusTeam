using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Courses
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? Provider { get; set; } // Internal, Udemy, Coursera

        [StringLength(500)]
        public string? ExternalLink { get; set; }

        public int? EstimatedHours { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<EmployeeCourses> EmployeeCourses { get; set; } = new List<EmployeeCourses>();
    }
}