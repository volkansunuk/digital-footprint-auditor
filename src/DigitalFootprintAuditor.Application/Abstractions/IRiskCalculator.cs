using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IRiskCalculator
{
    int CalculateScore(IEnumerable<ScanFinding> findings);

    RiskLevel CalculateRiskLevel(int score);
}