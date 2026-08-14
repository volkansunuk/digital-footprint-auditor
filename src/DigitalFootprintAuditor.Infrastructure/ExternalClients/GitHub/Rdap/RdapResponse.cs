using System.Text.Json.Serialization;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Rdap;

public class RdapResponse
{
    [JsonPropertyName("ldhName")]
    public string? LdhName { get; set; }

    [JsonPropertyName("events")]
    public List<RdapEvent> Events { get; set; } = new();

    [JsonPropertyName("status")]
    public List<string> Status { get; set; } = new();
}

public class RdapEvent
{
    [JsonPropertyName("eventAction")]
    public string EventAction { get; set; } = string.Empty;

    [JsonPropertyName("eventDate")]
    public DateTime EventDate { get; set; }
}