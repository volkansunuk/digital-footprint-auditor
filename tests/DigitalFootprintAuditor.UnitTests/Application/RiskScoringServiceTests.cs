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
            new(){ScoreImpact = 10},  //Public Email
            new(){ScoreImpact = 15},  // DMARC
            new(){ScoreImpact = 30}   //HTTPS
        };
        var result = _service.CalculateScore(findings);

        Assert.Equal(55, result);
    }

    [Fact]
    public void CalculateScore_ShouldReturnZero_WhenFindingsAreEmpty()
    {
        var findings = Array.Empty<ScanFinding>();

        var result = _service.CalculateScore(findings);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateScore_ShouldClampScoreToOneHundred()
    {
        var findings = new List<ScanFinding>
        {
            new() { ScoreImpact = 60 },
            new() { ScoreImpact = 50 }
        };

        var result = _service.CalculateScore(findings);

        Assert.Equal(100, result);
    }

    [Fact]
    public void CalculateScore_ShouldIgnoreNegativeScoreImpacts()
    {
        var findings = new List<ScanFinding>
        {
            new() { ScoreImpact = 10 },
            new() { ScoreImpact = -20 }
        };

        var result = _service.CalculateScore(findings);

        Assert.Equal(10, result);
    }

    [Fact]
    public void CalculateScore_ShouldThrowArgumentNullException_WhenFindingsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _service.CalculateScore(null!));
    }

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(20, RiskLevel.Low)]
    [InlineData(21, RiskLevel.Medium)]
    [InlineData(50, RiskLevel.Medium)]
    [InlineData(51, RiskLevel.High)]
    [InlineData(100, RiskLevel.High)]
    public void CalculateRiskLevel_ShouldReturnExpectedLevel(int score, RiskLevel expectedLevel)
    {
        var result = _service.CalculateRiskLevel(score);

        Assert.Equal(expectedLevel, result);
    }
}