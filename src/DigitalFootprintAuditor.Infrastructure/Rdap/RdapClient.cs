using System.Net;
using System.Net.Http.Json;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Infrastructure.Rdap.Models;


namespace DigitalFootprintAuditor.Infrastructure.Rdap;

public sealed class RdapClient : IRdapClient
{
    private readonly HttpClient _httpClient;

    public RdapClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RdapDomainResult?> GetDomainAsync(
        string domain,
        CancellationToken cancellationToken)
    {
        var escapedDomain = Uri.EscapeDataString(domain);

        try
        {
            using var response = await _httpClient.GetAsync(
                $"domain/{escapedDomain}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var rdapResponse =
                await response.Content.ReadFromJsonAsync<RdapResponse>(
                    cancellationToken);

            if (rdapResponse is null)
            {
                return null;
            }

            return MapToResult(rdapResponse, domain);
        }

        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "RDAP isteği zaman aşımına uğradı.",
                exception);
        }
    }

    private static RdapDomainResult MapToResult(
        RdapResponse response,
        string requestedDomain)
    {
        return new RdapDomainResult(
            Domain: response.Domain ?? requestedDomain,
            RegistrationDate: GetEventDate(
                response,
                "registration"),
            UpdatedDate: GetEventDate(
                response,
                "last changed"),
            ExpirationDate: GetEventDate(
                response,
                "expiration"),
            Registrar: null,
            Nameservers: response.Nameservers
                .Where(server =>
                    !string.IsNullOrWhiteSpace(server.Name))
                .Select(server => server.Name!)
                .ToArray());
    }

    private static DateTimeOffset? GetEventDate(
        RdapResponse response,
        string action)
    {
        return response.Events
            .FirstOrDefault(item =>
                string.Equals(
                    item.Action,
                    action,
                    StringComparison.OrdinalIgnoreCase))
            ?.Date;
    }
}