using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class AssetIncidents
    {
        [Key]
        public int IncidentId { get; set; }

        [Required]
        public int AssetId { get; set; }

        public int? EmployeeId { get; set; }

        [Required]
        [StringLength(50)]
        public string IncidentType { get; set; } = string.Empty; // LOST, BROKEN, DAMAGED

        [Required]
        public DateOnly IncidentDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ReportedBy { get; set; }

        public DateTime ReportedDate { get; set; } = DateTime.Now;

        public DateOnly? ResolvedDate { get; set; }

        [StringLength(500)]
        public string? ResolutionNotes { get; set; }

        // Navigation properties
        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; } = null!;

        [ForeignKey("EmployeeId")]
        public virtual Employees? Employee { get; set; }

        [ForeignKey("ReportedBy")]
        public virtual Employees? Reporter { get; set; }
    }
}