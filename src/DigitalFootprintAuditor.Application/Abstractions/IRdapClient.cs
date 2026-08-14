
using DigitalFootprintAuditor.Application.Models;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IRdapClient
{
    Task<RdapDomainResult?> GetDomainAsync(
        string domain,
        CancellationToken cancellationToken);
}