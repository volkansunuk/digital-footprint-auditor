using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Security;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class GravatarScanner : IScanner
{
    private readonly IGravatarClient _gravatarClient;

    public GravatarScanner(IGravatarClient gravatarClient)
    {
        _gravatarClient = gravatarClient;
    }

    //***
    public IReadOnlyCollection<TargetType> SupportedTargetTypes =>
        new[] { TargetType.Email };

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
                "GravatarScanner yalnızca Email hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        var emailHash = EmailHasher.Hash(target.TargetValue);

        try
        {
            var profile = await _gravatarClient.GetProfileAsync(
                emailHash,
                cancellationToken);

            if (profile is null)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    "Gravatar Profili Bulunamadı",
                    "Bu e-posta adresinin hash değeriyle eşleşen herkese açık bir Gravatar profili bulunamadı. " +
                    "Bu sonuç bilgi amaçlıdır ve risk puanını artırmaz.",
                    FindingSeverity.Info,
                    0));

                return findings;
            }

            findings.Add(CreateFinding(
                target.ScanId,
                "Gravatar Profili Bulundu",
                "Bu e-posta adresinin hash değeriyle eşleşen herkese açık bir Gravatar profili bulundu. " +
                "Bu bulgu, e-posta adresiyle bağlantılı görünür bir dijital kimlik izi bulunduğunu gösterir. " +
                "Mevcut eğitim modelinde bu bulgu risk puanını artırmaz.",
                FindingSeverity.Info,
                0));

            return findings;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                "Gravatar API Zaman Aşımına Uğradı",
                "Gravatar API isteği belirlenen süre içinde tamamlanamadı.",
                FindingSeverity.Low,
                0));

            return findings;
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                "Gravatar API'ye Ulaşılamadı",
                "Gravatar API ile iletişim kurulurken bir HTTP veya bağlantı hatası oluştu.",
                FindingSeverity.Low,
                0));

            return findings;
        }
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
            ScannerName = nameof(GravatarScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "Gravatar API",
            CreatedAt = DateTime.UtcNow
        };
    }
}