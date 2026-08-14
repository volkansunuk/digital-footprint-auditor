using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public class ScanFindingResult
{
    public string ScannerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public int ScoreImpact { get; set; }
    public string Source { get; set; } = string.Empty;
}