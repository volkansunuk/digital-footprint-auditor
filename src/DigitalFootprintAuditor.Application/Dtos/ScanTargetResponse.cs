using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Dtos;

public class ScanTargetResponse
{
    public Guid Id { get; set; }
    public ScanTargetType TargetType { get; set; }
    public string TargetValue { get; set; } = string.Empty;
}