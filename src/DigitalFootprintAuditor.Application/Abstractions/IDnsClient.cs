using DigitalFootprintAuditor.Application.Models;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IDnsClient
{
    Task<DnsRecordResult> GetRecordsAsync(
        string domain,
        CancellationToken cancellationToken);
}