using DigitalFootprintAuditor.Application.Dtos;
namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IScanService
{
    Task<ScanResponseDto> CreateScanAsync(CreateScanRequestDto request, CancellationToken cancellationToken); 
    Task<ScanResponseDto?> GetScanByIdAsync(Guid scanId, CancellationToken cancellationToken); 
    Task<IReadOnlyCollection<ScanResponseDto>> GetAllScansAsync(CancellationToken cancellationToken); 
    Task<bool> DeleteScanAsync(Guid scanId, CancellationToken cancellationToken); 

    Task<Guid> PrepareScanAsync(
    CreateScanRequestDto request,
    CancellationToken cancellationToken);

    Task<ScanResponseDto> RunScanAsync(
        Guid scanId,
        CancellationToken cancellationToken);    
}