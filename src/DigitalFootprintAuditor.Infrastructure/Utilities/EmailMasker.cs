namespace DigitalFootprintAuditor.Infrastructure.Utilities;

public static class EmailMasker
{
    public static string Mask(string email)
    {
        var atIndex = email.IndexOf('@');

        if (atIndex <= 1)
        {
            return email;
        }

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];

        var maskedLocalPart = localPart[0] + new string('*', localPart.Length - 2) + localPart[^1];

        return maskedLocalPart + domainPart;
    }
}