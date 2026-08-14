using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public class RiskScoringResult
{
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
}

public interface IRiskScoringService
{
    RiskScoringResult CalculateRisk(IEnumerable<int> findingScoreImpacts);
}