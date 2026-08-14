using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Domain.Entities;

public class ScanFinding
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }
    public string ScannerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public int ScoreImpact { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Scan? Scan { get; set; }
}