namespace LotusTeam.Models
{
    public class RefreshTokens
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }

        public virtual User User { get; set; } = null!;
    }
}