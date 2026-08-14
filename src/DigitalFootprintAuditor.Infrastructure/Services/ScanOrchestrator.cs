using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalFootprintAuditor.Infrastructure.Services;

public class ScanOrchestrator : IScanOrchestrator
{
    private readonly IEnumerable<IFootprintScanner> _scanners;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(IEnumerable<IFootprintScanner> scanners, ILogger<ScanOrchestrator> logger)
    {
        _scanners = scanners;
        _logger = logger;
    }

    public async Task<ScanOrchestrationResult> RunScannersAsync(
        ICollection<ScanTarget> targets,
        CancellationToken cancellationToken)
    {
        var result = new ScanOrchestrationResult();

        foreach (var target in targets)
        {
            var matchingScanners = _scanners.Where(s => s.SupportedTargetType == target.TargetType);

            foreach (var scanner in matchingScanners)
            {
                try
                {
                    var findingResults = await scanner.ScanAsync(target.TargetValue, cancellationToken);
                    result.Findings.AddRange(findingResults);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Scanner {ScannerType} hedef {TargetValue} için beklenmedik bir hatayla başarısız oldu.",
                        scanner.GetType().Name,
                        target.TargetValue);

                    result.HadUnexpectedFailures = true;

                    result.Findings.Add(new ScanFindingResult
                    {
                        ScannerName = scanner.GetType().Name,
                        Title = "Tarama sırasında beklenmedik bir hata oluştu",
                        Description = $"'{target.TargetValue}' hedefi taranırken bir hata oluştu, bu hedef için sonuçlar eksik olabilir.",
                        Severity = Domain.Enums.FindingSeverity.Info,
                        ScoreImpact = 0,
                        Source = "System"
                    });
                }
            }
        }

        return result;
    }
}