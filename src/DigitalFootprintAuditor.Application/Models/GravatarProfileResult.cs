namespace DigitalFootprintAuditor.Application.Models;

public sealed record GravatarProfileResult(
    string ProfileUrl,
    string? DisplayName,
    string? PreferredUsername
);