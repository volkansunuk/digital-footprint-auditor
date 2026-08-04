using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Services;

public class RiskScoringService
{
    public int CalculateScore(IEnumerable<ScanFinding> findings)
    {
        return findings.Sum(finding => finding.ScoreImpact);
    }

    public RiskLevel CalculateRiskLevel(int score)
    {
        if (score >= 51)
        {
            return RiskLevel.High;
        }

        if (score >= 21)
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }
}
