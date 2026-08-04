using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class DnsSecurityScanner : IScanner
{
    private readonly IDnsClient _dnsClient;

    public DnsSecurityScanner(IDnsClient dnsClient)
    {
        _dnsClient = dnsClient;
    }

    public TargetType SupportedTargetType => TargetType.Domain;

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
                "DnsSecurityScanner yalnızca Domain hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        try
        {
            var records = await _dnsClient.GetRecordsAsync(
                target.TargetValue,
                cancellationToken);

            if (records.ARecords.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "A kaydı bulunamadı",
                    description:
                        "Domain için A kaydı bulunamadı. Bu, alan adının bir IPv4 adresine yönlendirilmemiş olabileceğini gösterebilir.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }

            if (records.AaaaRecords.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "AAAA kaydı bulunamadı",
                    description:
                        "Domain için AAAA kaydı bulunamadı. Bu, alan adının IPv6 üzerinden erişilemediğini gösterebilir.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }

            if (records.MxRecords.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "MX kaydı bulunamadı",
                    description:
                        "Domain için MX kaydı bulunamadı. Bu, domain adına bağlı bir e-posta sunucusunun yapılandırılmamış olabileceğini gösterebilir.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }

            var hasSpf = records.TxtRecords.Any(record =>
                record.TrimStart().StartsWith(
                    "v=spf1",
                    StringComparison.OrdinalIgnoreCase));

            if (!hasSpf)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "SPF kaydı bulunamadı",
                    description:
                        "Domain için SPF kaydı bulunamadı. SPF, hangi sunucuların bu domain adına e-posta gönderebileceğini belirtir ve e-posta sahteciliğini azaltmaya yardımcı olur.",
                    severity: FindingSeverity.Low,
                    scoreImpact: 10));
            }

            var hasDmarc = records.DmarcRecords.Any(record =>
                record.TrimStart().StartsWith(
                    "v=DMARC1",
                    StringComparison.OrdinalIgnoreCase));

            if (!hasDmarc)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "DMARC kaydı bulunamadı",
                    description:
                        "Domain için DMARC kaydı bulunamadı. DMARC, SPF ve DKIM kontrolleri başarısız olduğunda e-postaların nasıl işleneceğini belirleyerek e-posta sahteciliğine karşı koruma sağlamaya yardımcı olur.",
                    severity: FindingSeverity.Medium,
                    scoreImpact: 15));
            }

            if (findings.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "DNS güvenlik kayıtları bulundu",
                    description:
                        "Domain için temel DNS kayıtları ile SPF ve DMARC kayıtları bulundu.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));
            }
        }
        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "DNS sorgusu zaman aşımına uğradı",
                description:
                    "DNS servisi belirlenen süre içinde cevap vermedi.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "DNS servisine ulaşılamadı",
                description:
                    "DNS kayıtları sorgulanırken bir bağlantı veya servis hatası oluştu.",
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
            ScannerName = nameof(DnsSecurityScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "DNS",
            CreatedAt = DateTime.UtcNow
        };
    }
}