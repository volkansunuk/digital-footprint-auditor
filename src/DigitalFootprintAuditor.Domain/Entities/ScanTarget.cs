using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Domain.Entities;

public class ScanTarget
{
    public Guid Id { get; set; }
    public Guid ScanId { get; set; }
    public ScanTargetType TargetType { get; set; }
    public string TargetValue { get; set; } = string.Empty;

    public Scan? Scan { get; set; }
}