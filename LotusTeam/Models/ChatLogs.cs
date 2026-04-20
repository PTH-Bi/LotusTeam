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
}