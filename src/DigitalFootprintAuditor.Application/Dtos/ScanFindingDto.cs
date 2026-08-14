namespace DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;

public record ScanFindingDto 
(
    Guid Id,
    string ScannerName,
    string Title,
    string Description,
    FindingSeverity Severity,
    int ScoreImpact,
    string Source,
    DateTime CreatedAt,
    string Recommendation
);
