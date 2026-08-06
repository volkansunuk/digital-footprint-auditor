using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.GitHub.Models;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class GitHubProfileScanner : IScanner
{
    private readonly GitHubClient _gitHubClient;

    public GitHubProfileScanner(GitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }
    //***
    public IReadOnlyCollection<TargetType> SupportedTargetTypes =>
        new[] { TargetType.GitHubUsername };

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
                "GitHubProfileScanner yalnızca GitHubUsername hedeflerini işler.",
                nameof(target));
        }

        var findings = new List<ScanFinding>();

        try
        {
            var profile = await _gitHubClient.GetUserAsync(
                target.TargetValue,
                cancellationToken);

            if (profile is null)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "GitHub Profili Bulunamadı",
                    description:
                        $"'{target.TargetValue}' kullanıcı adına ait herkese açık bir GitHub profili bulunamadı.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));

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
                        "GitHub profilinde herkese açık bir e-posta adresi bulunuyor. " +
                        "Bu, kişisel bilgilerin daha kolay erişilebilir hale gelmesi nedeniyle risk oluşturan bir bulgudur.",
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
        }
        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub API Zaman Aşımına Uğradı",
                description:
                    "GitHub API isteği belirlenen süre içinde tamamlanamadı.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }
        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub API'ye Ulaşılamadı",
                description:
                    "GitHub API ile iletişim kurulurken bir bağlantı veya HTTP hatası oluştu.",
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