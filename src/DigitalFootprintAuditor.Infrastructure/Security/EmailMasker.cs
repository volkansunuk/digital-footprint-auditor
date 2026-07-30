namespace DigitalFootprintAuditor.Infrastructure.Security;

public static class EmailMasker
{
    public static string Mask(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var trimmedEmail = email.Trim();
        var atIndex = trimmedEmail.IndexOf('@');

        if (atIndex <= 0 || atIndex == trimmedEmail.Length - 1)
        {
            throw new ArgumentException(
                  "Email must contain a valid local part and domain",
                  nameof(email));
        }

        var localPart = trimmedEmail[..atIndex];
        var domainPart = trimmedEmail[(atIndex + 1)..];

        return $"{localPart[0]}***@{domainPart}";
    }
}