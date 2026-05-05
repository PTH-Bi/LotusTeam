using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class WorkflowSteps
    {
        [Key]
        public int StepId { get; set; }

        [Required]
        public int WorkflowId { get; set; }

        [Required]
        [StringLength(100)]
        public string StepName { get; set; } = string.Empty;

        [Required]
        public int StepOrder { get; set; }

        public int? RoleId { get; set; }

        public int? UserId { get; set; }

        public bool IsMandatory { get; set; } = true;

        // Navigation properties
        [ForeignKey("WorkflowId")]
        public virtual Workflows Workflow { get; set; } = null!;

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}