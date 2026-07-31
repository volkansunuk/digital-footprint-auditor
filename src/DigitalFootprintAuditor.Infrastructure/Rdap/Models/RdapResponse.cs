using System.IO.Pipes;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client;

namespace DigitalFootprintAuditor.Infrastructure.Rdap.Models;

public sealed class RdapResponse
{
    [JsonPropertyName("ldhName")]
    public string? Domain{get; set;}

    [JsonPropertyName("events")]
    public List<RdapEvent> Events { get; set; } = [];

    [JsonPropertyName("nameservers")]
    public List<RdapNameServer> Nameservers { get; set; } = [];
}

public sealed class RdapEvent
{
    [JsonPropertyName("eventAction")]
    public string? Action { get; set; }

    [JsonPropertyName("eventDate")]
    public DateTimeOffset? Date { get; set; }
}

public sealed class RdapNameServer
{
    [JsonPropertyName("ldhName")]
    public string? Name { get; set; }
}