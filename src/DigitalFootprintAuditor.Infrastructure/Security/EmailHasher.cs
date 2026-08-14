using System.Security.Cryptography;
using System.Text;

namespace DigitalFootprintAuditor.Infrastructure.Security;

public static class EmailHasher
{
    public static string Hash(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var emailBytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hashBytes = SHA256.HashData(emailBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}