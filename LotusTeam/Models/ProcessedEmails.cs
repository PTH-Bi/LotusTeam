namespace LotusTeam.Models
{
    public class ProcessedEmails
    {
        public int Id { get; set; }

        public string MessageId { get; set; } = null!;

        public string? SenderEmail { get; set; }

        public string? Subject { get; set; }

        public DateTime ProcessedAt { get; set; }
    }
}