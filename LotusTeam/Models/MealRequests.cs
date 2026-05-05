using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class MealRequests
    {
        [Key]
        public int MealRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateOnly RequestDate { get; set; }

        [StringLength(50)]
        public string? MealType { get; set; } // BREAKFAST, LUNCH, DINNER

        public int Quantity { get; set; } = 1;

        [StringLength(200)]
        public string? Notes { get; set; }

        public short StatusId { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public int? RequestId { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees Employee { get; set; } = null!;

        [ForeignKey("StatusId")]
        public virtual StatusMasters? Status { get; set; }

        [ForeignKey("RequestId")]
        public virtual Requests? Request { get; set; } // Đảm bảo có class Request trong namespace LotusTeam.Core.Entities

        [ForeignKey("ApprovedBy")]
        public virtual Employees? Approver { get; set; }
    }
}