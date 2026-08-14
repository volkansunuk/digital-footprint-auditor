namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IGitHubApiClient
{
    Task<GitHubUserResult?> GetUserInfoAsync(string username, CancellationToken cancellationToken);
}

public class GitHubUserResult
{
    public string Login { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public DateTime CreatedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
}