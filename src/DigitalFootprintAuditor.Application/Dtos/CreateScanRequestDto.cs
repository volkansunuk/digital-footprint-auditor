namespace DigitalFootprintAuditor.Application.Dtos;

using DigitalFootprintAuditor.Domain.Enums;

/* nuraları class kullanarak yapmayı düşünmüştüm ama recordda veri yapısı değiştirilemez(immutable) 
yani ilk atandıktan sonra değiştirilemez ve söz dizimi daha kolay kısa*/

/* Kullanıcının yeni tarama başlatırken göndereceği veriler (Çoklu hedef destekli) */
public record CreateScanRequestDto(
    IReadOnlyCollection<ScanTargetInputDto> Targets
);