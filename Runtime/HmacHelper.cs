using System.Security.Cryptography;
using System.Text;

namespace GameDevPartner.SDK
{
    /// <summary>
    /// HMAC-SHA256 helper for request signing.
    /// </summary>
    public static class HmacHelper
    {
        public static string ComputeHmacSha256(string message, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(messageBytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
