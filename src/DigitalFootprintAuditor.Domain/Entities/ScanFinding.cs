namespace DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
public class ScanFinding
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid ScanId {get; set;} 
    public string ScannerName {get; set;} = string.Empty;
    public string Title {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;
    public FindingSeverity Severity {get; set;} =FindingSeverity.Info;
    public int ScoreImpact {get; set;}
    public string Source {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}