using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;


namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class RdapDomainScanner : IScanner
{
    private readonly IRdapClient _rdapClient;

    public  RdapDomainScanner(IRdapClient rdapClient)
    {
        _rdapClient = rdapClient;
    }

    public TargetType SupportedTargetType => TargetType.Domain;

    public async Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target, 
        CancellationToken cancellationToken)
    {
         if (target.TargetType != SupportedTargetType)
        {
            throw new ArgumentException(
                "GravatarScanner yalnızca Domain hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();
        string normalizedDomain;

        try
        {
            normalizedDomain = NormalizeDomain(target.TargetValue);
        }
        catch (ArgumentException exception)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                "Geçersiz Domain",
                exception.Message,
                FindingSeverity.Info,
                0));

            return findings;
        }

        try
        {
            var result = await _rdapClient.GetDomainAsync(
                normalizedDomain,
                cancellationToken);

            if (result is null)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    "Domain Kaydı Bulunamadı",
                    $"{normalizedDomain} için RDAP kaydı bulunamadı.",
                    FindingSeverity.Info,
                    0));

                return findings;
            }

            var description =
                $"Domain: {result.Domain}. " +
                $"Kayıt tarihi: {FormatDate(result.RegistrationDate)}. " +
                $"Son güncelleme: {FormatDate(result.UpdatedDate)}. " +
                $"Nameserver sayısı: {result.Nameservers.Count}.";

            findings.Add(CreateFinding(
                target.ScanId,
                "Domain RDAP Kaydı Bulundu",
                description,
                FindingSeverity.Info,
                0));

            return findings;
        }
        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                "RDAP İsteği Zaman Aşımına Uğradı",
                "RDAP servisi belirlenen süre içinde cevap vermedi.",
                FindingSeverity.Low,
                0));

            return findings;
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                "RDAP Servisine Ulaşılamadı",
                "RDAP servisiyle iletişim kurulurken hata oluştu.",
                FindingSeverity.Low,
                0));

            return findings;
        }
    }

    public static string NormalizeDomain(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                "Domain boş bırakılamaz.",
                nameof(input));
        }

        var value = input.Trim();

        if (!value.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            value = "https://" + value;
        }

        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri))
        {
            throw new ArgumentException(
                "Geçerli bir domain girilmelidir.",
                nameof(input));
        }

        var host = uri.Host.TrimEnd('.');

        if (string.IsNullOrWhiteSpace(host) ||
            Uri.CheckHostName(host) != UriHostNameType.Dns)
        {
            throw new ArgumentException(
                "Geçerli bir domain girilmelidir.",
                nameof(input));
        }

        return host.ToLowerInvariant();
    }

    private static string FormatDate(DateTimeOffset? date)
    {
        return date?.ToString("yyyy-MM-dd") ?? "bilinmiyor";
    }

    private static ScanFinding CreateFinding(
        Guid scanId,
        string title,
        string description,
        FindingSeverity severity,
        int scoreImpact)
    {
        return new ScanFinding
        {
            ScanId = scanId,
            ScannerName = nameof(RdapDomainScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "RDAP",
            CreatedAt = DateTime.UtcNow
        };
    }

}

    


