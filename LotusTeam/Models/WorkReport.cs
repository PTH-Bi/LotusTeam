using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class WorkReport
    {
        [Key]
        public int ReportID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("EmployeeID")]
        public Employees? Employee { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }
    }
}