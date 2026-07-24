namespace DigitalFootprintAuditor.Application.Abstractions;

using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;

public interface IScanner
{
    // Bu scanner'ın desteklediği hedef türünü belirtir (örn: TargetType.GitHubUsername).
    TargetType SupportedTargetType { get; }

    // Verilen hedef değerini ilgili dış serviste tarar ve tespit edilen bulguların listesini döner.
    // <param name="targetValue">Taranacak metinsel değer (örn: "octocat")</param>
    // <param name="cancellationToken">Tarama iptal edildiğinde dış API çağrısının da iptal edilebilmesi için</param>
    Task<IEnumerable<ScanFindingDto>> ScanAsync(string targetValue, CancellationToken cancellationToken);
}