using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class InternalAnnouncement
    {
        [Key]
        public int AnnouncementId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public int? AuthorId { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public bool IsPinned { get; set; } = false;

        public DateTime PublishDate { get; set; } = DateTime.Now;

        public DateTime? ExpireDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("AuthorId")]
        public virtual Employees? Author { get; set; }
    }
}