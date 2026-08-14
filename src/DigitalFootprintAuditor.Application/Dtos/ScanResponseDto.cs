namespace DigitalFootprintAuditor.Application.Dtos;

using DigitalFootprintAuditor.Domain.Enums;

public record ScanResponseDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    ScanStatus Status,
    int RiskScore,
    RiskLevel RiskLevel,
    IReadOnlyCollection<ScanTargetInputDto> Targets,
    IReadOnlyCollection<ScanFindingDto> Findings,
    IReadOnlyCollection<ScannerStatusDto> ScannerStatuses
);