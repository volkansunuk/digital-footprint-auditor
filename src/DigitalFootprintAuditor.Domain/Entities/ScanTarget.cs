namespace DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
public class ScanTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScanId {get; set;} 
    public TargetType TargetType {get; set;}
    public string TargetValue {get; set;} = string.Empty;
}