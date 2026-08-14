using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Dtos;

public class ScanResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ScanStatus Status { get; set; }
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public List<ScanTargetResponse> Targets { get; set; } = new();
    public List<FindingResponse> Findings { get; set; } = new();
}