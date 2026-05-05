namespace LotusTeam.DTOs
{
    public class AnnouncementDto
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int? AuthorId { get; set; }
        public string Category { get; set; }
        public bool IsPinned { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public bool IsActive { get; set; }
    }
}