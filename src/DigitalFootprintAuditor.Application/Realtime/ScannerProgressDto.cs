namespace DigitalFootprintAuditor.Application.Realtime;

public record ScannerProgressDto(
    Guid ScanId,
    string ScannerName,
    ScannerProgressStatus Status
);