using System.Net;
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

    public async Task<GitHubUserResponse?> GetUserAsync(
        string username,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var escapedUsername = Uri.EscapeDataString(username.Trim());

        try
        {
            using var response = await _httpClient.GetAsync($"users/{escapedUsername}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new HttpRequestException(
                    "GitHub API request was forbidden. The rate limit may have been exceeded.",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

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

    public async Task<IReadOnlyCollection<GitHubRepositoryResponse>?> GetRepositoriesAsync(
        string username,
        CancellationToken cancellationToken)
        {
                ArgumentException.ThrowIfNullOrWhiteSpace(username);

            var escapedUsername =
                Uri.EscapeDataString(username.Trim());

            try
            {
                using var response = await _httpClient.GetAsync(
                    $"users/{escapedUsername}/repos?per_page=100",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HttpRequestException(
                        "GitHub API request was forbidden. " +
                        "The rate limit may have been exceeded.",
                        inner: null,
                        statusCode: response.StatusCode);
                }

                response.EnsureSuccessStatusCode();

                var repositories =
                    await response.Content
                        .ReadFromJsonAsync<List<GitHubRepositoryResponse>>(
                            cancellationToken: cancellationToken);

                return repositories is null
                    ? Array.Empty<GitHubRepositoryResponse>()
                    : repositories;
            }
            
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "The GitHub repository request timed out.",
                    exception);
            }

    }
}
