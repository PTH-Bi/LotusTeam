namespace LotusTeam.Models
{
    public class AuditLog
    {
        public long LogID { get; set; }
        public int? UserID { get; set; }
        public string ActionType { get; set; } = null!;
        public string? TableName { get; set; }
        public string? RecordID { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime LogTime { get; set; }

        public User? User { get; set; }
    }

}
