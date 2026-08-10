namespace DigitalFootprintAuditor.Application.Dtos;

public record ScannerStatusDto(
    string ScannerName,
    bool IsSuccessful
);