namespace DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;

/* taramada bulunan açığın dışarı sunulacak detayları*/
public record ScanFindingDto 
(
    Guid Id,
    string ScannerName,
    string Title,
    string Description,
    FindingSeverity Severity,
    int ScoreImpact,
    string Source,
    DateTime CreatedAt
);
