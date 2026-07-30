//Bu model, Gravatar’ın ham JSON cevabı değildir. 
//Scanner’ın ihtiyaç duyduğu sadeleştirilmiş sonucu temsil eder.

namespace DigitalFootprintAuditor.Application.Models;

public sealed record GravatarProfileResult(
    string ProfileUrl,
    string? DisplayName,
    string? PreferredUsername
);