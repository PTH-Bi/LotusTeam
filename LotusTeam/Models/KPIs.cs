using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Models
{
    public class KPIs
    {
        [Key]
        public int KpiId { get; set; }

        [Required]
        [StringLength(50)]
        public string KpiCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string KpiName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Formula { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; }

        [Precision(10, 2)]
        public decimal? TargetValue { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<EmployeeKPIs> EmployeeKPIs { get; set; } = new List<EmployeeKPIs>();
    }
}