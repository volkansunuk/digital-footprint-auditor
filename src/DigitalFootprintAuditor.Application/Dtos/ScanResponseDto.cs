namespace DigitalFootprintAuditor.Application.Dtos;

using DigitalFootprintAuditor.Domain.Enums;

//tarama sonucu dönerken kullanacağımız ana Dto
public record ScanResponseDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    ScanStatus Status,
    int RiskScore,
    RiskLevel RiskLevel,
    IReadOnlyCollection<ScanTargetInputDto> Targets,
    List<ScanFindingDto> Findings
);