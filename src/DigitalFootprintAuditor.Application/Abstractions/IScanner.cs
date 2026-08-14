using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IScanner
{
    IReadOnlyCollection<TargetType> SupportedTargetTypes { get; }
     Task<IReadOnlyCollection<ScanFinding>> ScanAsync(ScanTarget target, CancellationToken cancellationToken);
}