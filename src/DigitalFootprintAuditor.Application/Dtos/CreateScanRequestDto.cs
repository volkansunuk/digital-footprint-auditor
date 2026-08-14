namespace DigitalFootprintAuditor.Application.Dtos;

public record CreateScanRequestDto(
    IReadOnlyCollection<ScanTargetInputDto> Targets
);