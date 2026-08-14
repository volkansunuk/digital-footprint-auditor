using System.Text.Json.Serialization;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Dns;

public class DnsOverHttpsResponse
{
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    [JsonPropertyName("Answer")]
    public List<DnsAnswer>? Answer { get; set; }
}

public class DnsAnswer
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}