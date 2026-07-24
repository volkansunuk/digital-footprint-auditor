namespace DigitalFootprintAuditor.Application.Dtos;

using DigitalFootprintAuditor.Domain.Enums;

//hedef girdi modeli
public record ScanTargetInputDto(
    TargetType TargetType,
    string TargetValue
);