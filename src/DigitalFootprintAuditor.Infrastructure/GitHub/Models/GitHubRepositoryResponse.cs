

using System.Text.Json.Serialization;

namespace DigitalFootprintAuditor.Infrastructure.GitHub.Models;

public sealed record GitHubRepositoryResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("fork")]
    public bool IsFork { get; init; }

    [JsonPropertyName("archived")]
    public bool IsArchived { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init;}

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init;}
}