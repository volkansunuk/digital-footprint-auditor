using DigitalFootprintAuditor.Domain.Entities;

namespace DigitalFootprintAuditor.Application.Abstractions;

public class ScanOrchestrationResult
{
    public List<ScanFindingResult> Findings { get; set; } = new();
    public bool HadUnexpectedFailures { get; set; }
}

public interface IScanOrchestrator
{
    Task<ScanOrchestrationResult> RunScannersAsync(
        ICollection<ScanTarget> targets,
        CancellationToken cancellationToken);
}