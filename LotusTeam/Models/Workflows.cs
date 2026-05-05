using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam. Models
{
    public class Workflows
    {
        [Key]
        public int WorkflowId { get; set; }

        [Required]
        [StringLength(50)]
        public string WorkflowCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string WorkflowName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<WorkflowSteps> WorkflowSteps { get; set; } = new List<WorkflowSteps>();

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}