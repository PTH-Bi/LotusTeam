using System.Text;

namespace LotusTeam.Helpers
{
    public static class TokenEncryption
    {
        public static string Encrypt(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string Decrypt(string encrypted)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
        }
    }
}