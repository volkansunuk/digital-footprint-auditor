using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class HttpsScanner : IScanner
{
    private readonly IWebsiteSecurityClient _websiteSecurityClient;

    public HttpsScanner(IWebsiteSecurityClient websiteSecurityClient)
    {
        _websiteSecurityClient = websiteSecurityClient;
    }

    public TargetType SupportedTargetType => TargetType.Website;

    public async Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target,
        CancellationToken cancellationToken)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (target.TargetType != SupportedTargetType)
        {
            throw new ArgumentException(
                "HttpsScanner yalnızca Website hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        try
        {
            var result = await _websiteSecurityClient.GetSecurityInfoAsync(
                target.TargetValue,
                cancellationToken);

            if (!result.UsesHttps)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "HTTPS kullanılmıyor",
                    description:
                        "Web sitesi güvenli HTTPS bağlantısı kullanmıyor.",
                    severity: FindingSeverity.High,
                    scoreImpact: 30));
            }

            if (!result.RedirectsToHttps)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "HTTP adresi HTTPS'e yönlenmiyor",
                    description:
                        "Web sitesinin HTTP adresi otomatik olarak HTTPS adresine yönlenmiyor.",
                    severity: FindingSeverity.Medium,
                    scoreImpact: 10));
            }

            if (findings.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "HTTPS yapılandırması uygun",
                    description:
                        "Web sitesi HTTPS kullanıyor ve HTTP isteklerini HTTPS'e yönlendiriyor.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }
        }
        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "Web sitesi kontrolü zaman aşımına uğradı",
                description:
                    "Web sitesi belirlenen süre içinde cevap vermedi.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "Web sitesine ulaşılamadı",
                description:
                    "Web sitesine bağlanılırken HTTP veya sertifika kaynaklı bir hata oluştu.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }

        return findings;
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
            Id = Guid.NewGuid(),
            ScanId = scanId,
            ScannerName = nameof(HttpsScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "Website",
            CreatedAt = DateTime.UtcNow
        };
    }
}