using DigitalFootprintAuditor.Application.Dtos;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IScanService
{
    Task<ScanResponse> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken);
    Task<ScanResponse?> GetScanByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ScanResponse>> GetAllScansAsync(CancellationToken cancellationToken);
    Task<bool> DeleteScanAsync(Guid id, CancellationToken cancellationToken);
}