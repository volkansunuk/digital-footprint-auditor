using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Infrastructure.Services;

public class RiskScoringService : IRiskScoringService
{
    private const int MaxRiskScore = 100;
    private const int LowRiskUpperBound = 20;
    private const int MediumRiskUpperBound = 50;

    public RiskScoringResult CalculateRisk(IEnumerable<int> findingScoreImpacts)
    {
        var rawScore = findingScoreImpacts.Sum();
        var cappedScore = Math.Min(rawScore, MaxRiskScore);

        var riskLevel = cappedScore switch
        {
            <= LowRiskUpperBound => RiskLevel.Low,
            <= MediumRiskUpperBound => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        return new RiskScoringResult
        {
            RiskScore = cappedScore,
            RiskLevel = riskLevel
        };
    }
}