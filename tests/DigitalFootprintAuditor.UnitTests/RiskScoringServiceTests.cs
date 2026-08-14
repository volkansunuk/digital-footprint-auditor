using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Services;

namespace DigitalFootprintAuditor.UnitTests.Services;

public class RiskScoringServiceTests
{
    private readonly RiskScoringService _service = new();

    [Theory]
    [InlineData(20, RiskLevel.Low)]
    [InlineData(21, RiskLevel.Medium)]
    [InlineData(50, RiskLevel.Medium)]
    [InlineData(51, RiskLevel.High)]
    public void CalculateRisk_BoundaryScores_ReturnsExpectedRiskLevel(int score, RiskLevel expectedLevel)
    {
        var result = _service.CalculateRisk([score]);

        Assert.Equal(score, result.RiskScore);
        Assert.Equal(expectedLevel, result.RiskLevel);
    }

    [Fact]
    public void CalculateRisk_ScoreExceedsMaximum_CapsScoreAtOneHundred()
    {
        var result = _service.CalculateRisk([75, 50]);

        Assert.Equal(100, result.RiskScore);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
    }
}
