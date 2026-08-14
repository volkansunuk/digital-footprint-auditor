namespace DigitalFootprintAuditor.Application.Abstractions;

public class SecurityHeaderResult
{
    public bool RedirectsToHttps { get; set; }
    public bool HasHsts { get; set; }
    public bool HasXContentTypeOptions { get; set; }
    public bool HasXFrameOptions { get; set; }
    public bool HasContentSecurityPolicy { get; set; }
}

public interface ISecurityHeaderClient
{
    Task<SecurityHeaderResult> CheckAsync(string domain, CancellationToken cancellationToken);
}