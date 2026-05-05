namespace LotusTeam.Models
{
    public class ChatLogs
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatFeedback
    {
        public int Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public bool IsHelpful { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Faq
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}