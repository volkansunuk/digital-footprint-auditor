namespace DigitalFootprintAuditor.Application.Dtos;

using DigitalFootprintAuditor.Domain.Enums;

public record ScanTargetInputDto(
    TargetType TargetType,
    string TargetValue
);