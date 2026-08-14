namespace DigitalFootprintAuditor.Infrastructure.Utilities;

public static class DomainNormalizer
{
    public static string Normalize(string input)
    {
        var trimmed = input.Trim().ToLowerInvariant();

        // Şema varsa (http://, https://) kaldır
        if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
        {
            var uri = new Uri(trimmed);
            trimmed = uri.Host;
        }

        // Başındaki "www." varsa kaldır
        if (trimmed.StartsWith("www."))
        {
            trimmed = trimmed[4..];
        }

        // Sonunda "/" varsa kaldır
        trimmed = trimmed.TrimEnd('/');

        return trimmed;
    }
}