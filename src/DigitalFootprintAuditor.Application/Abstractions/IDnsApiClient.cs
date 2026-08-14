namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IDnsApiClient
{
    Task<List<string>> GetTxtRecordsAsync(string domain, CancellationToken cancellationToken);
}