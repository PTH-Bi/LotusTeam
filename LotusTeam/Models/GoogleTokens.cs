namespace LotusTeam.Models
{
    public class GoogleTokens
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
