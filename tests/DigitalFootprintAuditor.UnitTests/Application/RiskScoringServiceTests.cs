using DigitalFootprintAuditor.Application.Services;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.UnitTests.Application;

public class RiskScoringServiceTests
{
    private readonly RiskScoringService _service = new();

    [Fact]
    public void CalculateScore_ShouldSumAllScoreImpacts()
    {
        var findings = new List<ScanFinding>
        {
            new(){ScoreImpact = 5},
            new(){ScoreImpact = 10},
            new(){ScoreImpact = 3}
        };
        var result = _service.CalculateScore(findings);

        Assert.Equal(18, result);
    }

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(20, RiskLevel.Low)]
    [InlineData(21, RiskLevel.Medium)]
    [InlineData(50, RiskLevel.Medium)]
    [InlineData(51, RiskLevel.High)]
    public void CalculateRiskLevel_ShouldReturnExpectedLevel(int score, RiskLevel expectedLevel)
    {
        var result = _service.CalculateRiskLevel(score);

        Assert.Equal(expectedLevel, result);
    }
}