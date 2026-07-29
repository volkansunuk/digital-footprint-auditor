using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.GitHub;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

// Gün 7 — GitHub scanner geliştirme ve API response mapping
//
// [x] GitHubClient üzerinden profil bilgisini ister.
// [x] GitHub response modelini Domain ScanFinding nesnelerine dönüştürür.
// [x] Bilgilendirme bulguları ile mahremiyet risklerini ayırır.
// [x] CancellationToken değerini GitHubClient'a iletir.

public sealed class GitHubProfileScanner : IScanner
{
    private readonly GitHubClient _gitHubClient;

    public GitHubProfileScanner(GitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    public TargetType SupportedTargetType => TargetType.GitHubUsername;

    public async Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target,
        CancellationToken cancellationToken)
    {
        if (target.TargetType != SupportedTargetType)
        {
            throw new ArgumentException(
                "GitHubProfileScanner yalnızca GitHubUsername hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        var profile = await _gitHubClient.GetUserAsync(
            target.TargetValue,
            cancellationToken);

        // 404 ve diğer başarısız HTTP sonuçları Gün 8'de ayrıca yönetilecek.
        if (profile is null)
        {
            return findings;
        }

        findings.Add(CreateFinding(
            target.ScanId,
            title: "GitHub Profili Tespit Edildi",
            description:
                $"'{profile.Login}' kullanıcı adlı GitHub profili bulundu. " +
                $"Public repository sayısı: {profile.PublicRepos}.",
            severity: FindingSeverity.Info,
            scoreImpact: 0));

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "Herkese Açık E-Posta Adresi Bulundu",
                description:
                    "GitHub profilinde herkese açık bir e-posta adresi bulunuyor.",
                severity: FindingSeverity.Medium,
                scoreImpact: 10));
        }

        if (!string.IsNullOrWhiteSpace(profile.Bio))
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub Profil Biyografisi Mevcut",
                description:
                    "GitHub profilinde herkese açık bir biyografi bilgisi bulunuyor.",
                severity: FindingSeverity.Info,
                scoreImpact: 0));
        }

        findings.Add(CreateFinding(
            target.ScanId,
            title: "GitHub Hesap Bilgileri",
            description:
                $"Hesap oluşturulma tarihi: {profile.CreatedAt:yyyy-MM-dd}. " +
                $"Son güncelleme tarihi: {profile.UpdatedAt:yyyy-MM-dd}.",
            severity: FindingSeverity.Info,
            scoreImpact: 0));

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
            ScanId = scanId,
            ScannerName = nameof(GitHubProfileScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "GitHub API",
            CreatedAt = DateTime.UtcNow
        };
    }
}