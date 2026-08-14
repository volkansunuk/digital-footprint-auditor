using System.Net.Http.Json;
using DigitalFootprintAuditor.Application.Abstractions;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Dns;

public class DnsApiClient : IDnsApiClient
{
    private readonly HttpClient _httpClient;

    public DnsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> GetTxtRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"resolve?name={domain}&type=TXT", cancellationToken);

        response.EnsureSuccessStatusCode();

        var dnsResponse = await response.Content.ReadFromJsonAsync<DnsOverHttpsResponse>(cancellationToken);

        if (dnsResponse?.Answer is null)
        {
            return new List<string>();
        }

        return dnsResponse.Answer
            .Select(a => a.Data.Trim('"'))
            .ToList();
    }
}