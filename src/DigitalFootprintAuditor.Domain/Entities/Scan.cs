namespace DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
public class Scan
{
    public Guid Id {get; set;} = Guid.NewGuid(); 
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow; 
    public DateTime? CompletedAt {get; set;} 
    public ScanStatus Status {get; set;} = ScanStatus.Pending;
    public int RiskScore {get; set;}
    public RiskLevel RiskLevel {get; set;}
    public ICollection<ScanTarget> Targets { get; set; } = new List<ScanTarget>(); 
    public ICollection<ScanFinding> Findings { get; set; } = new List<ScanFinding>();
}