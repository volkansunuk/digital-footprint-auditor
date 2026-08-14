using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.GitHub;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public class GitHubExposureScanner : IFootprintScanner
{
    private readonly IGitHubApiClient _gitHubApiClient;

    public GitHubExposureScanner(IGitHubApiClient gitHubApiClient)
    {
        _gitHubApiClient = gitHubApiClient;
    }

    public ScanTargetType SupportedTargetType => ScanTargetType.GitHubUsername;

    public async Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingResult>();

        GitHubUserResult? userInfo;

        try
        {
            userInfo = await _gitHubApiClient.GetUserInfoAsync(targetValue, cancellationToken);
        }
        catch (GitHubRateLimitExceededException)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GitHubExposureScanner),
                Title = "GitHub taraması geçici olarak yapılamadı",
                Description = "GitHub API istek sınırı aşıldı, bu hedef şu anda taranamadı. Lütfen daha sonra tekrar deneyin.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "GitHub API"
            });
            return findings;
        }
        catch (HttpRequestException ex)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GitHubExposureScanner),
                Title = "GitHub taraması başarısız oldu",
                Description = $"GitHub API'sine bağlanırken bir hata oluştu: {ex.Message}",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "GitHub API"
            });
            return findings;
        }

        if (userInfo is null)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GitHubExposureScanner),
                Title = "GitHub kullanıcısı bulunamadı",
                Description = $"'{targetValue}' adında bir GitHub kullanıcısı bulunamadı.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "GitHub API"
            });
            return findings;
        }

        var accountAgeInDays = (DateTime.UtcNow - userInfo.CreatedAt).TotalDays;
        if (accountAgeInDays < 30)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GitHubExposureScanner),
                Title = "Yeni oluşturulmuş GitHub hesabı",
                Description = $"Hesap {(int)accountAgeInDays} gün önce oluşturulmuş, bu bazı bağlamlarda güven sinyali olarak değerlendirilebilir.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "GitHub API"
            });
        }

        if (userInfo.PublicRepos > 50)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GitHubExposureScanner),
                Title = "Yüksek sayıda açık repository",
                Description = $"Kullanıcının {userInfo.PublicRepos} adet açık (public) repository'si var, bu potansiyel bilgi sızıntısı yüzeyini artırabilir.",
                Severity = FindingSeverity.Medium,
                ScoreImpact = 10,
                Source = "GitHub API"
            });
        }

        findings.Add(new ScanFindingResult
        {
            ScannerName = nameof(GitHubExposureScanner),
            Title = "GitHub profili bulundu",
            Description = $"Profil: {userInfo.HtmlUrl}, {userInfo.PublicRepos} public repo, {userInfo.Followers} takipçi.",
            Severity = FindingSeverity.Info,
            ScoreImpact = 0,
            Source = "GitHub API"
        });

        return findings;
    }
}