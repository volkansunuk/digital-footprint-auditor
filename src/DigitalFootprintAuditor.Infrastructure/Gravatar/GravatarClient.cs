using System.Net;
using System.Net.Http.Json;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Infrastructure.Gravatar.Models;

namespace DigitalFootprintAuditor.Infrastructure.Gravatar;

public sealed class GravatarClient : IGravatarClient
{
    private readonly HttpClient _httpClient;
    public GravatarClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<GravatarProfileResult?> GetProfileAsync(
        string emailHash,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"profiles/{emailHash}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        var gravatarResponse = await response.Content.ReadFromJsonAsync<GravatarProfileResponse>(cancellationToken);

        if (gravatarResponse is null)
        {
            return null;
        }
        return new GravatarProfileResult(
            gravatarResponse.ProfileUrl ?? string.Empty,
            gravatarResponse.DisplayName,
            gravatarResponse.PreferredUsername
        );

    }
}
