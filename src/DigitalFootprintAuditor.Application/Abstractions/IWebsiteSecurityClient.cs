using DigitalFootprintAuditor.Application.Models;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IWebsiteSecurityClient
{
    Task<WebsiteSecurityResult> GetSecurityInfoAsync(
        string domain,
        CancellationToken cancellationToken);
}