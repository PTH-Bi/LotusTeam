using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class EmployeeKPIs
    {
        [Key]
        public int EmployeeKpiId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int KpiId { get; set; }

        [Required]
        public DateOnly Period { get; set; }

        [Precision(10, 2)]
        public decimal? ActualValue { get; set; }

        [Precision(5, 2)]
        public decimal? Score { get; set; }

        public int? EvaluatedBy { get; set; }

        public DateTime? EvaluatedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("KpiId")]
        public virtual KPIs KPI { get; set; } = null!;

        [ForeignKey("EvaluatedBy")]
        public virtual Employees? Evaluator { get; set; }
    }
}