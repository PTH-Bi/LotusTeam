using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace LotusTeam.Models
{
    public class OKRs
    {
        [Key]
        public int OkrId { get; set; }

        [Required]
        [StringLength(500)]
        public string Objective { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }

        public int? DepartmentId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Precision(5, 2)]
        public decimal Progress { get; set; } = 0;

        public short? StatusId { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<KeyResults> KeyResults { get; set; } = new List<KeyResults>();

        [ForeignKey("EmployeeId")]
        public virtual Employees? Employee { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
    }
}