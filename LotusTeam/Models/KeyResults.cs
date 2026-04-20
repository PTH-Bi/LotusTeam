using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class KeyResults
    {
        [Key]
        public int KrId { get; set; }

        [Required]
        public int OkrId { get; set; }

        [Required]
        [StringLength(500)]
        public string ResultDescription { get; set; } = string.Empty; // Đổi tên từ KeyResult thành ResultDescription

        [StringLength(200)]
        public string? TargetValue { get; set; }

        [StringLength(200)]
        public string? CurrentValue { get; set; }

        [Precision(5, 2)]
        public decimal Progress { get; set; } = 0;

        [Precision(5, 2)]
        public decimal Weight { get; set; } = 1;

        // Navigation property
        [ForeignKey("OkrId")]
        public virtual OKRs OKR { get; set; } = null!;
    }
}