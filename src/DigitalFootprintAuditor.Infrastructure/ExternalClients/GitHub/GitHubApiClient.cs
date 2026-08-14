using System.Net;
using System.Net.Http.Json;
using DigitalFootprintAuditor.Application.Abstractions;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.GitHub;

public class GitHubApiClient : IGitHubApiClient
{
    private readonly HttpClient _httpClient;
public GitHubApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
  public async Task<GitHubUserResult?> GetUserInfoAsync(string username, CancellationToken cancellationToken)
{
    var response = await _httpClient.GetAsync($"users/{username}", cancellationToken);

    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }

    if (response.StatusCode == HttpStatusCode.Forbidden)
    {
        throw new GitHubRateLimitExceededException(
            "GitHub API rate limit aşıldı. Lütfen daha sonra tekrar deneyin.");
    }

    response.EnsureSuccessStatusCode();

    var gitHubResponse = await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken);

    if (gitHubResponse is null)
    {
        return null;
    }

    return new GitHubUserResult
    {
        Login = gitHubResponse.Login,
        PublicRepos = gitHubResponse.PublicRepos,
        Followers = gitHubResponse.Followers,
        Following = gitHubResponse.Following,
        CreatedAt = gitHubResponse.CreatedAt,
        HtmlUrl = gitHubResponse.HtmlUrl
    };
 }  
}