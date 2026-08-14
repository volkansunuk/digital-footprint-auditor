using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public class HttpsSecurityScanner : IFootprintScanner
{
    private readonly ISecurityHeaderClient _securityHeaderClient;

    public HttpsSecurityScanner(ISecurityHeaderClient securityHeaderClient)
    {
        _securityHeaderClient = securityHeaderClient;
    }

    public ScanTargetType SupportedTargetType => ScanTargetType.Website;

    public async Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingResult>();
        var normalizedDomain = DomainNormalizer.Normalize(targetValue);

        SecurityHeaderResult result;

try
{
    result = await _securityHeaderClient.CheckAsync(normalizedDomain, cancellationToken);
}
catch (Exception ex) when (HttpExceptionHelper.IsTimeout(ex, cancellationToken))
{
    findings.Add(new ScanFindingResult
    {
        ScannerName = nameof(HttpsSecurityScanner),
        Title = "HTTPS/Security Header taraması zaman aşımına uğradı",
        Description = $"'{normalizedDomain}' adresinden belirlenen sürede yanıt alınamadı.",
        Severity = FindingSeverity.Info,
        ScoreImpact = 0,
        Source = "HTTP Headers"
    });
    return findings;
}
catch (HttpRequestException ex)
{
    findings.Add(new ScanFindingResult
    {
        ScannerName = nameof(HttpsSecurityScanner),
        Title = "HTTPS/Security Header taraması başarısız oldu",
        Description = $"'{normalizedDomain}' adresine bağlanırken bir hata oluştu: {ex.Message}",
        Severity = FindingSeverity.Info,
        ScoreImpact = 0,
        Source = "HTTP Headers"
    });
    return findings;
}

        if (!result.RedirectsToHttps)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(HttpsSecurityScanner),
                Title = "HTTPS zorunlu değil",
                Description = $"'{normalizedDomain}', http:// isteğini otomatik olarak https://'ye yönlendirmiyor. Bu, trafiğin şifresiz iletilmesine izin verebilir.",
                Severity = FindingSeverity.Medium,
                ScoreImpact = 15,
                Source = "HTTP Headers"
            });
        }

        if (!result.HasHsts)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(HttpsSecurityScanner),
                Title = "HSTS header'ı eksik",
                Description = $"'{normalizedDomain}', Strict-Transport-Security header'ı göndermiyor. Bu header, tarayıcıya her zaman HTTPS kullanmasını söyler.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "HTTP Headers"
            });
        }

        if (!result.HasXContentTypeOptions)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(HttpsSecurityScanner),
                Title = "X-Content-Type-Options header'ı eksik",
                Description = $"'{normalizedDomain}', X-Content-Type-Options: nosniff header'ı göndermiyor.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "HTTP Headers"
            });
        }

        if (!result.HasXFrameOptions)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(HttpsSecurityScanner),
                Title = "X-Frame-Options header'ı eksik",
                Description = $"'{normalizedDomain}', X-Frame-Options header'ı göndermiyor. Bu, clickjacking saldırılarına karşı koruma eksikliği anlamına gelebilir.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "HTTP Headers"
            });
        }

        if (!result.HasContentSecurityPolicy)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(HttpsSecurityScanner),
                Title = "Content-Security-Policy header'ı eksik",
                Description = $"'{normalizedDomain}', Content-Security-Policy header'ı göndermiyor.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "HTTP Headers"
            });
        }

        return findings;
    }
}