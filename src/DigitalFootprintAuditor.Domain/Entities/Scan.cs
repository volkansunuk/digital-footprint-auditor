using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Domain.Entities;

public class Scan
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ScanStatus Status { get; set; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }

    public ICollection<ScanTarget> Targets { get; set; } = new List<ScanTarget>();
    public ICollection<ScanFinding> Findings { get; set; } = new List<ScanFinding>();
}  