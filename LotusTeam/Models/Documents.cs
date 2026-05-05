using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DocumentCategory { get; set; } // Thêm mới

        public int? EmployeeId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public int? FileSize { get; set; }

        public int? UploadedBy { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        public DateOnly? ExpiryDate { get; set; }

        public bool IsConfidential { get; set; } = false;

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employees? Employee { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }
    }
}