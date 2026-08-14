using System.Net;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Gravatar;

public class GravatarApiClient : IGravatarApiClient
{
    private readonly HttpClient _httpClient;

    public GravatarApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> HasGravatarAsync(string email, CancellationToken cancellationToken)
    {
        var emailHash = EmailHasher.ComputeSha256Hash(email);

        var response = await _httpClient.GetAsync($"avatar/{emailHash}?d=404", cancellationToken);

        return response.StatusCode == HttpStatusCode.OK;
    }
}