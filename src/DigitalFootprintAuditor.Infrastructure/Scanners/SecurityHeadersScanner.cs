using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class SecurityHeadersScanner : IScanner
{
    private readonly IWebsiteSecurityClient _websiteSecurityClient;

    public SecurityHeadersScanner(
        IWebsiteSecurityClient websiteSecurityClient)
    {
        _websiteSecurityClient = websiteSecurityClient;
    }

    public IReadOnlyCollection<TargetType> SupportedTargetTypes =>
        new[] 
        { 
            TargetType.Domain,
            TargetType.Website
        };

    public async Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target,
        CancellationToken cancellationToken)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (!SupportedTargetTypes.Contains(target.TargetType))
        {
            throw new ArgumentException(
                "SecurityHeadersScanner yalnızca Domain veya Website hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        try
        {
            var result =
                await _websiteSecurityClient.GetSecurityInfoAsync(
                    target.TargetValue,
                    cancellationToken);

            if (!result.Headers.ContainsKey(
                    "Strict-Transport-Security"))
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "HSTS başlığı bulunamadı",
                    description:
                        "Strict-Transport-Security başlığı bulunamadı. " +
                        "Bu başlık, tarayıcının siteye yalnızca HTTPS üzerinden bağlanmasını zorunlu hâle getirmeye yardımcı olur.",
                    severity: FindingSeverity.Low,
                    scoreImpact: 5));
            }

            if (!result.Headers.ContainsKey(
                    "Content-Security-Policy"))
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title:
                        "Content-Security-Policy başlığı bulunamadı",
                    description:
                        "Content-Security-Policy başlığı bulunamadı. " +
                        "Bu başlık, tarayıcının hangi kaynaklardan içerik yükleyebileceğini sınırlandırarak içerik enjeksiyonu risklerini azaltmaya yardımcı olur.",
                    severity: FindingSeverity.Low,
                    scoreImpact: 5));
            }

            if (!result.Headers.ContainsKey(
                    "X-Content-Type-Options"))
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title:
                        "X-Content-Type-Options başlığı bulunamadı",
                    description:
                        "X-Content-Type-Options başlığı bulunamadı. " +
                        "Bu başlık, tarayıcının içerik türünü tahmin etmesini engellemeye yardımcı olur.",
                    severity: FindingSeverity.Low,
                    scoreImpact: 0));
            }

            if (!result.Headers.ContainsKey(
                    "Referrer-Policy"))
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "Referrer-Policy başlığı bulunamadı",
                    description:
                        "Referrer-Policy başlığı bulunamadı. " +
                        "Bu başlık, başka sitelere yönlendirme sırasında ne kadar adres bilgisinin paylaşılacağını sınırlandırır.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }

            if (!result.Headers.ContainsKey(
                    "Permissions-Policy"))
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title:
                        "Permissions-Policy başlığı bulunamadı",
                    description:
                        "Permissions-Policy başlığı bulunamadı. " +
                        "Bu başlık; kamera, mikrofon ve konum gibi tarayıcı özelliklerinin kullanımını sınırlandırmaya yardımcı olur.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }

            if (findings.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "Temel güvenlik başlıkları bulundu",
                    description:
                        "Web sitesi kontrol edilen temel HTTP güvenlik başlıklarını kullanıyor.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }
        }
        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title:
                    "Güvenlik başlıkları kontrolü zaman aşımına uğradı",
                description:
                    "Web sitesi belirlenen süre içinde cevap vermediği için güvenlik başlıkları kontrol edilemedi.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title:
                    "Web sitesinin güvenlik başlıkları kontrol edilemedi",
                description:
                    "Web sitesine bağlanılırken bir bağlantı, HTTP veya sertifika hatası oluştu.",
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
            ScannerName = nameof(SecurityHeadersScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "Website Security Headers",
            CreatedAt = DateTime.UtcNow
        };
    }
}