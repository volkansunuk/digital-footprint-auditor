using DigitalFootprintAuditor.Application.Abstractions;

namespace DigitalFootprintAuditor.Infrastructure.ExternalClients.Security;

public class SecurityHeaderClient : ISecurityHeaderClient
{
    private readonly HttpClient _httpClient;

    public SecurityHeaderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SecurityHeaderResult> CheckAsync(string domain, CancellationToken cancellationToken)
    {
        var result = new SecurityHeaderResult();

        // Önce http:// ile dene, yönlendirme https'e mi gidiyor bak
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"http://{domain}");
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        result.RedirectsToHttps = httpResponse.RequestMessage?.RequestUri?.Scheme == "https";

        // Header kontrolleri için https cevabını kullan (yönlendirme sonrası nihai cevap)
        result.HasHsts = httpResponse.Headers.Contains("Strict-Transport-Security");
        result.HasXContentTypeOptions = httpResponse.Headers.Contains("X-Content-Type-Options");
        result.HasXFrameOptions = httpResponse.Headers.Contains("X-Frame-Options");
        result.HasContentSecurityPolicy = httpResponse.Headers.Contains("Content-Security-Policy");

        return result;
    }
}