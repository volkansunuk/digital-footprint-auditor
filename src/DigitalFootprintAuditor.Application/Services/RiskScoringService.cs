using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Application.Abstractions;

namespace DigitalFootprintAuditor.Application.Services;

public class RiskScoringService : IRiskCalculator
{
    public int CalculateScore(
        IEnumerable<ScanFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        var totalScore = findings.Sum(
            finding => Math.Max(
                0,
                finding.ScoreImpact));

        return Math.Min(totalScore, 100);
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
