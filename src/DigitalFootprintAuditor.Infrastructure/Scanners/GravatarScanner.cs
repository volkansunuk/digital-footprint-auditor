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

    public TargetType SupportedTargetType => TargetType.Email;

    public async Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target,
        CancellationToken cancellationToken)
    {
        if (target.TargetType != SupportedTargetType)
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
                    "Bu e-posta hash'i için herkese açık bir Gravatar profili bulunamadı.",
                    FindingSeverity.Info,
                    0));

                return findings;
            }

            findings.Add(CreateFinding(
                target.ScanId,
                "Gravatar Profili Bulundu",
                "Bu e-posta hash'i ile eşleşen herkese açık bir Gravatar profili bulundu. " +
                "Bu bulgu, görünür bir dijital kimlik izinin olduğunu gösterir ve risk puanlamasında dikkate alınır.",
                FindingSeverity.Info,
                5));

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