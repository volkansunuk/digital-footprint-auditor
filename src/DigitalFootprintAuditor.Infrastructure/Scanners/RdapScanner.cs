using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public class RdapScanner : IFootprintScanner
{
    private readonly IRdapApiClient _rdapApiClient;

    public RdapScanner(IRdapApiClient rdapApiClient)
    {
        _rdapApiClient = rdapApiClient;
    }

    public ScanTargetType SupportedTargetType => ScanTargetType.Domain;

    public async Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingResult>();

        string normalizedDomain;

        try
        {
            normalizedDomain = DomainNormalizer.Normalize(targetValue);
        }
        catch (UriFormatException)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "Geçersiz domain formatı",
                Description = $"'{targetValue}' geçerli bir domain veya URL formatında değil.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "RDAP API"
            });
            return findings;
        }

        RdapDomainResult? domainInfo;

        try
        {
            domainInfo = await _rdapApiClient.GetDomainInfoAsync(normalizedDomain, cancellationToken);
        }
        catch (Exception ex) when (HttpExceptionHelper.IsTimeout(ex, cancellationToken))
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "RDAP taraması zaman aşımına uğradı",
                Description = $"'{normalizedDomain}' için RDAP sunucusundan belirlenen sürede yanıt alınamadı.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "RDAP API"
            });
            return findings;
        }
        catch (HttpRequestException ex)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "RDAP taraması başarısız oldu",
                Description = $"RDAP API'sine bağlanırken bir hata oluştu: {ex.Message}",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "RDAP API"
            });
            return findings;
        }

        if (domainInfo is null)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "Domain kaydı bulunamadı",
                Description = $"'{normalizedDomain}' için RDAP üzerinden bir kayıt bulunamadı.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "RDAP API"
            });
            return findings;
        }

        if (domainInfo.RegistrationDate is not null)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "Domain kayıt bilgisi bulundu",
                Description = $"'{normalizedDomain}' domain'i {domainInfo.RegistrationDate:yyyy-MM-dd} tarihinde kayıt edilmiş.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "RDAP API"
            });
        }

        if (domainInfo.ExpirationDate is not null && domainInfo.ExpirationDate < DateTime.UtcNow.AddDays(30))
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(RdapScanner),
                Title = "Domain süresi yakında doluyor",
                Description = $"'{normalizedDomain}' domain'inin süresi {domainInfo.ExpirationDate:yyyy-MM-dd} tarihinde doluyor, bu bir güvenlik riski oluşturabilir (domain devralma - domain hijacking).",
                Severity = FindingSeverity.Medium,
                ScoreImpact = 10,
                Source = "RDAP API"
            });
        }

        return findings;
    }
}