namespace DigitalFootprintAuditor.Infrastructure.Scanners;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;

public class GitHubProfileScanner : IScanner
{
    private readonly HttpClient _httpClient;

    public GitHubProfileScanner(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public TargetType SupportedTargetType => TargetType.GitHubUsername;

    public async Task<IEnumerable<ScanFindingDto>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingDto>();

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync($"users/{targetValue}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // GitHub'a hiç ulaşılamadı: DNS çözümlenemedi, bağlantı reddedildi, ağ kopması vb.
            findings.Add(new ScanFindingDto(
                Id: Guid.NewGuid(),
                ScannerName: nameof(GitHubProfileScanner),
                Title: "GitHub API'ye Ulaşılamadı",
                Description: $"GitHub API'ye bağlanırken bir ağ hatası oluştu: {ex.Message}",
                Severity: FindingSeverity.Low,
                ScoreImpact: 0,
                Source: "GitHub API",
                CreatedAt: DateTime.UtcNow
            ));

            return findings;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Kullanıcı taramayı iptal etmedi, demek ki HttpClient.Timeout süresi doldu.
            findings.Add(new ScanFindingDto(
                Id: Guid.NewGuid(),
                ScannerName: nameof(GitHubProfileScanner),
                Title: "GitHub API Zaman Aşımına Uğradı",
                Description: $"GitHub API isteği belirlenen süre içinde cevap vermedi: {ex.Message}",
                Severity: FindingSeverity.Low,
                ScoreImpact: 0,
                Source: "GitHub API",
                CreatedAt: DateTime.UtcNow
            ));

            return findings;
        }

        // 1. Kullanıcı Bulunamadı Durumu (404)
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            findings.Add(new ScanFindingDto(
                Id: Guid.NewGuid(),
                ScannerName: nameof(GitHubProfileScanner),
                Title: "GitHub Kullanıcısı Bulunamadı",
                Description: $"'{targetValue}' kullanıcı adına sahip bir GitHub profili bulunamadı.",
                Severity: FindingSeverity.Info,
                ScoreImpact: 0,
                Source: "GitHub API",
                CreatedAt: DateTime.UtcNow
            ));

            return findings;
        }

        // İsteğin başarısız olduğu diğer durumlar (örn: Rate Limit / 403)
        if (!response.IsSuccessStatusCode)
        {
            findings.Add(new ScanFindingDto(
                Id: Guid.NewGuid(),
                ScannerName: nameof(GitHubProfileScanner),
                Title: "GitHub API Hatası",
                Description: $"GitHub API isteği başarısız oldu. Durum Kodu: {response.StatusCode}",
                Severity: FindingSeverity.Low,
                ScoreImpact: 0,
                Source: "GitHub API",
                CreatedAt: DateTime.UtcNow
            ));

            return findings;
        }

        // 2. Kullanıcı Bulundu - Profil Bilgilerini Çözümleme
        var profile = await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: cancellationToken);

        if (profile != null)
        {
            // Bulgu 1: Profil Mevcut (Saf bilgilendirme - risk puanına katkısı yok)
            // Doküman uyarısı: GitHub aktivitesi (repo sayısı vb.) risk değildir.
            findings.Add(new ScanFindingDto(
                Id: Guid.NewGuid(),
                ScannerName: nameof(GitHubProfileScanner),
                Title: "GitHub Profili Tespit Edildi",
                Description: $"'{targetValue}' kullanıcı adlı aktif bir GitHub profili bulundu. Public Repo Sayısı: {profile.PublicRepos}",
                Severity: FindingSeverity.Info,
                ScoreImpact: 0,
                Source: "GitHub API",
                CreatedAt: DateTime.UtcNow
            ));

            // Bulgu 2: Açık E-posta Adresi Varlığı (gerçek gizlilik riski)
            if (!string.IsNullOrEmpty(profile.Email))
            {
                findings.Add(new ScanFindingDto(
                    Id: Guid.NewGuid(),
                    ScannerName: nameof(GitHubProfileScanner),
                    Title: "Açık E-Posta Adresi Tespit Edildi",
                    Description: $"GitHub profilinde herkese açık e-posta adresi tespit edildi: {profile.Email}",
                    Severity: FindingSeverity.Medium,
                    ScoreImpact: 15,
                    Source: "GitHub API",
                    CreatedAt: DateTime.UtcNow
                ));
            }

            // Bulgu 3: Bio veya Lokasyon Bilgisi Paylaşımı (dijital ayak izi)
            if (!string.IsNullOrEmpty(profile.Bio) || !string.IsNullOrEmpty(profile.Location))
            {
                findings.Add(new ScanFindingDto(
                    Id: Guid.NewGuid(),
                    ScannerName: nameof(GitHubProfileScanner),
                    Title: "Kişisel Profil Detayları Açık",
                    Description: $"Profilde bio veya lokasyon bilgisi açıkça paylaşılmış. (Bio: {profile.Bio ?? "Yok"}, Lokasyon: {profile.Location ?? "Yok"})",
                    Severity: FindingSeverity.Low,
                    ScoreImpact: 5,
                    Source: "GitHub API",
                    CreatedAt: DateTime.UtcNow
                ));
            }
        }

        return findings;
    }
}

// GitHub API'den gelen JSON yanıtını eşlemek için dahili model
internal record GitHubUserResponse(
    [property: JsonPropertyName("login")] string Login,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("bio")] string? Bio,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("public_repos")] int PublicRepos
);