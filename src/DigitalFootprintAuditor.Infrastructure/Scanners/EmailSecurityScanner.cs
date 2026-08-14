using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public class EmailSecurityScanner : IFootprintScanner
{
    private readonly IDnsApiClient _dnsApiClient;

    public EmailSecurityScanner(IDnsApiClient dnsApiClient)
    {
        _dnsApiClient = dnsApiClient;
    }

    public ScanTargetType SupportedTargetType => ScanTargetType.Domain;

    public async Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingResult>();
        var normalizedDomain = DomainNormalizer.Normalize(targetValue);

        List<string> txtRecords;
        List<string> dmarcRecords;

        try
        {
            txtRecords = await _dnsApiClient.GetTxtRecordsAsync(normalizedDomain, cancellationToken);
            dmarcRecords = await _dnsApiClient.GetTxtRecordsAsync($"_dmarc.{normalizedDomain}", cancellationToken);
        }
        catch (Exception ex) when (HttpExceptionHelper.IsTimeout(ex, cancellationToken))
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(EmailSecurityScanner),
                Title = "E-posta güvenliği taraması zaman aşımına uğradı",
                Description = $"'{normalizedDomain}' için DNS sorgusundan belirlenen sürede yanıt alınamadı.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "DNS (TXT)"
            });
            return findings;
        }
        catch (HttpRequestException ex)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(EmailSecurityScanner),
                Title = "E-posta güvenliği taraması başarısız oldu",
                Description = $"DNS sorgusu sırasında bir hata oluştu: {ex.Message}",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "DNS (TXT)"
            });
            return findings;
        }

        var hasSpf = txtRecords.Any(r => r.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));

        if (!hasSpf)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(EmailSecurityScanner),
                Title = "SPF kaydı bulunamadı",
                Description = $"'{normalizedDomain}' için bir SPF kaydı bulunamadı. Bu, e-posta sahteciliğine (spoofing) karşı koruma eksikliği olabilir.",
                Severity = FindingSeverity.Medium,
                ScoreImpact = 15,
                Source = "DNS (TXT)"
            });
        }

        var dmarcRecord = dmarcRecords.FirstOrDefault(r => r.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));

        if (dmarcRecord is null)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(EmailSecurityScanner),
                Title = "DMARC kaydı bulunamadı",
                Description = $"'{normalizedDomain}' için bir DMARC kaydı bulunamadı. Bu, sahte e-postalara karşı politika/raporlama eksikliği anlamına gelir.",
                Severity = FindingSeverity.Medium,
                ScoreImpact = 15,
                Source = "DNS (TXT)"
            });
        }
        else if (dmarcRecord.Contains("p=none", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(EmailSecurityScanner),
                Title = "DMARC politikası zayıf (p=none)",
                Description = $"'{normalizedDomain}' domain'inin DMARC kaydı var, ama politika sadece izleme amaçlı (p=none), gerçek bir engelleme yapılmıyor.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "DNS (TXT)"
            });
        }

        return findings;
    }
}