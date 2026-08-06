using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Infrastructure.GitHub;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public sealed class GitHubRepositoryScanner : IScanner
{
    private readonly GitHubClient _gitHubClient;

    public GitHubRepositoryScanner(GitHubClient gitHubClient){
        _gitHubClient = gitHubClient;
    }

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
                "GitHubRepositoryScanner yalnızca GitHubUsername hedeflerini işler.", 
                nameof(target));
        }

        var findings =  new List<ScanFinding>();

        try
        {
            var repositories = await _gitHubClient.GetRepositoriesAsync(
                target.TargetValue,
                cancellationToken);

            if (repositories is null)
            {
               findings.Add(CreateFinding(
                    target.ScanId,
                    title: "GitHub Kullanıcısı Bulunamadı",
                    description:
                        $"'{target.TargetValue}' kullanıcı adına ait GitHub hesabı bulunamadığı için " +
                        "public repository bilgileri alınamadı. " +
                        "Bu sonuç bilgi amaçlıdır ve risk puanını artırmaz.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));

                return findings;
            }

            if (repositories.Count == 0)
            {
                findings.Add(CreateFinding(
                    target.ScanId,
                    title: "Herkese Açık Repository Bulunamadı",
                    description:
                        $"'{target.TargetValue}' kullanıcı adına ait herkese açık bir GitHub repository bulunamadı. " +
                        "Bu sonuç bilgi amaçlıdır ve risk puanını artırmaz.",
                    severity: FindingSeverity.Info,
                    scoreImpact: 0));

                return findings;
            }

            var archivedCount = 
                repositories.Count(repository =>
                    repository.IsArchived);

            var forkCount = 
                repositories.Count(repository =>
                    repository.IsFork);
            
            var latestUpdatedRepository =
                repositories
                    .OrderByDescending(repository =>
                         repository.UpdatedAt)
                    .First();

            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub Public Repository Özeti",
                description:
                    $"Toplam public repository sayısı: {repositories.Count}. " +
                    $"Fork repository sayısı: {forkCount}. " +
                    $"Arşivlenmiş repository sayısı: {archivedCount}. " +
                    $"En son güncellenen repository: '{latestUpdatedRepository.Name}', " +
                    $"güncelleme tarihi: {latestUpdatedRepository.UpdatedAt:yyyy-MM-dd}. " +
                    "Repository aktivitesi mevcut eğitim modelinde risk olarak değerlendirilmez " +
                    "ve risk puanını artırmaz.",
                severity: FindingSeverity.Info,
                scoreImpact: 0));

            return findings;
        }

        catch (TimeoutException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub API Zaman Aşımına Uğradı",
                description:
                    "GitHub repository bilgileri belirlenen süre içinde alınamadı.",
                severity: FindingSeverity.Low,
                scoreImpact: 0));
        }

        catch (HttpRequestException)
        {
            findings.Add(CreateFinding(
                target.ScanId,
                title: "GitHub Repository Bilgilerine Ulaşılamadı",
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
            Id = Guid.NewGuid(),
            ScanId = scanId,
            ScannerName = nameof(GitHubRepositoryScanner),
            Title = title,
            Description = description,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Source = "GitHub API",
            CreatedAt = DateTime.UtcNow
        };
    }
}

