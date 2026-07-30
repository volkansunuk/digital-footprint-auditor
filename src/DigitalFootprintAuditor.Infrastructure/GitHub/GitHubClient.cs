using System.Net; //HTTP durum kodlarını isimleriyle kullanabilmemizi sağlar.
using System.Net.Http.Json;
using DigitalFootprintAuditor.Infrastructure.GitHub.Models;

namespace DigitalFootprintAuditor.Infrastructure.GitHub;

public sealed class GitHubClient 
{
    private readonly HttpClient _httpClient; 
    public GitHubClient(HttpClient httpClient)
    {
        _httpClient = httpClient; 
    }

    public async Task<GitHubUserResponse?> GetUserAsync(string username, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var escapedUsername = Uri.EscapeDataString(username.Trim());

        try
        {
            using var response = await _httpClient.GetAsync($"users/{escapedUsername}", cancellationToken);

            //404 Not Found Durumu, GitHub kullanıcısı bulunamadı
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            
            //403 Rate Limit Durumu, GitHub isteği reddetti. Bunun sebebi rate limit olabilir.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new HttpRequestException(
                    "GitHub API request was forbidden. The rate limit may have been exceeded.",
                    inner: null,
                    statusCode: response.StatusCode);
            }
            
            // 404 ve 403 dışındaki başarısız HTTP cevaplarında exception fırlatır.
            response.EnsureSuccessStatusCode();

            // Başarılı cevabın JSON içeriğini GitHubUserResponse nesnesine dönüştürür.
            return await response.Content.ReadFromJsonAsync<GitHubUserResponse>(
            cancellationToken: cancellationToken);
        }

        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The GitHub API request timed out.", exception);
        }
    }
}

