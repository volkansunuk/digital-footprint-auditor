using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IScanner
{
    // Bu scanner'ın desteklediği hedef türünü belirtir (örn: TargetType.GitHubUsername).
    TargetType SupportedTargetType { get; }

    // Verilen hedef değerini ilgili dış serviste tarar ve tespit edilen bulguların listesini döner.
    // <param name="targetValue">Taranacak metinsel değer (örn: "octocat")</param>
    // <param name="cancellationToken">Tarama iptal edildiğinde dış API çağrısının da iptal edilebilmesi için</param>
     Task<IReadOnlyCollection<ScanFinding>> ScanAsync(ScanTarget target, CancellationToken cancellationToken);
}