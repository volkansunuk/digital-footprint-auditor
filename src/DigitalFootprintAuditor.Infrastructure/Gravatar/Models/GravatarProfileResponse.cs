using System.Text.Json.Serialization;
namespace DigitalFootprintAuditor.Infrastructure.Gravatar.Models;

public sealed record GravatarProfileResponse(
    
    [property: JsonPropertyName("profile_url")] string? ProfileUrl,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("preferred_username")] string? PreferredUsername
);
