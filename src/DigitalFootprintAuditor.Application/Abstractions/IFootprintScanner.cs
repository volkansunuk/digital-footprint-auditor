using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IFootprintScanner
{
    ScanTargetType SupportedTargetType { get; }
    Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken);
}