using System.Security.Cryptography;
using System.Text;

namespace DigitalFootprintAuditor.Infrastructure.Utilities;

public static class EmailHasher
{
    public static string ComputeSha256Hash(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var bytesToHash = Encoding.UTF8.GetBytes(normalizedEmail);
        var hashBytes = SHA256.HashData(bytesToHash);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}