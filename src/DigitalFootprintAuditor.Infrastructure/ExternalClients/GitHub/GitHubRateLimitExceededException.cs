namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.GitHub;

public class GitHubRateLimitExceededException : Exception
{
    public GitHubRateLimitExceededException(string message) : base(message)
    {
    }
}