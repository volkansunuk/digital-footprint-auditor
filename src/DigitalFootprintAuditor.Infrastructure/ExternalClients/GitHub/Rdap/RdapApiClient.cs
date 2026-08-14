using System.Net;
using System.Net.Http.Json;
using DigitalFootprintAuditor.Application.Abstractions;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Rdap;

public class RdapApiClient : IRdapApiClient
{
    private readonly HttpClient _httpClient;

    public RdapApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RdapDomainResult?> GetDomainInfoAsync(string domain, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"domain/{domain}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var rdapResponse = await response.Content.ReadFromJsonAsync<RdapResponse>(cancellationToken);

        if (rdapResponse is null)
        {
            return null;
        }

        return new RdapDomainResult
        {
            RegistrationDate = FindEventDate(rdapResponse, "registration"),
            ExpirationDate = FindEventDate(rdapResponse, "expiration"),
            LastChangedDate = FindEventDate(rdapResponse, "last changed")
        };
    }

    private static DateTime? FindEventDate(RdapResponse response, string eventAction)
    {
        var matchingEvent = response.Events
            .FirstOrDefault(e => e.EventAction.Equals(eventAction, StringComparison.OrdinalIgnoreCase));

        return matchingEvent?.EventDate;
    }
}