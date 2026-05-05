namespace LotusTeam.DTOs
{
    public class AnnouncementUpdateDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? ExpireDate { get; set; }
        public bool IsActive { get; set; }
    }
}