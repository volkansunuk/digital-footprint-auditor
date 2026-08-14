using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalFootprintAuditor.Infrastructure.Services;

public class ScanService : IScanService
{
    private readonly AuditorDbContext _dbContext;
private readonly IScanOrchestrator _scanOrchestrator;
private readonly IRiskScoringService _riskScoringService;

public ScanService(AuditorDbContext dbContext, IScanOrchestrator scanOrchestrator , IRiskScoringService riskScoringService )
{
    _dbContext = dbContext;
    _scanOrchestrator = scanOrchestrator;
    _riskScoringService = riskScoringService;
}

   public async Task<ScanResponse> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken)
{
    var scan = new Scan
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        Status = ScanStatus.Running,
        RiskScore = 0,
        RiskLevel = RiskLevel.Low
    };

    foreach (var targetRequest in request.Targets)
    {
        scan.Targets.Add(new ScanTarget
        {
            Id = Guid.NewGuid(),
            ScanId = scan.Id,
            TargetType = targetRequest.TargetType,
            TargetValue = targetRequest.TargetValue
        });
    }

    var orchestrationResult = await _scanOrchestrator.RunScannersAsync(scan.Targets, cancellationToken);

    foreach (var findingResult in orchestrationResult.Findings)
    {
        scan.Findings.Add(new ScanFinding
        {
            Id = Guid.NewGuid(),
            ScanId = scan.Id,
            ScannerName = findingResult.ScannerName,
            Title = findingResult.Title,
            Description = findingResult.Description,
            Severity = findingResult.Severity,
            ScoreImpact = findingResult.ScoreImpact,
            Source = findingResult.Source,
            CreatedAt = DateTime.UtcNow
        });
    }

    var riskResult = _riskScoringService.CalculateRisk(scan.Findings.Select(f => f.ScoreImpact));
scan.RiskScore = riskResult.RiskScore;
scan.RiskLevel = riskResult.RiskLevel;

    scan.Status = orchestrationResult.HadUnexpectedFailures
        ? ScanStatus.PartiallyCompleted
        : ScanStatus.Completed;
    scan.CompletedAt = DateTime.UtcNow;

    _dbContext.Scans.Add(scan);
    await _dbContext.SaveChangesAsync(cancellationToken);

    return MapToResponse(scan);
}

    public async Task<ScanResponse?> GetScanByIdAsync(Guid id, CancellationToken cancellationToken)
{
    var scan = await _dbContext.Scans
        .Include(s => s.Targets)
        .Include(s => s.Findings)
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    return scan is null ? null : MapToResponse(scan);
}

    public async Task<List<ScanResponse>> GetAllScansAsync(CancellationToken cancellationToken)
{
    var scans = await _dbContext.Scans
        .Include(s => s.Targets)
        .Include(s => s.Findings)
        .ToListAsync(cancellationToken);

    return scans.Select(MapToResponse).ToList();
}
public async Task<bool> DeleteScanAsync(Guid id, CancellationToken cancellationToken)
    {
        var scan = await _dbContext.Scans.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (scan is null)
        {
            return false;
        }

        _dbContext.Scans.Remove(scan);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    private static ScanResponse MapToResponse(Scan scan)
{
    return new ScanResponse
    {
        Id = scan.Id,
        CreatedAt = scan.CreatedAt,
        CompletedAt = scan.CompletedAt,
        Status = scan.Status,
        RiskScore = scan.RiskScore,
        RiskLevel = scan.RiskLevel,
        Targets = scan.Targets.Select(t => new ScanTargetResponse
        {
            Id = t.Id,
            TargetType = t.TargetType,
            TargetValue = t.TargetValue
        }).ToList(),
        Findings = scan.Findings.Select(f => new FindingResponse
        {
            Id = f.Id,
            ScannerName = f.ScannerName,
            Title = f.Title,
            Description = f.Description,
            Severity = f.Severity,
            ScoreImpact = f.ScoreImpact,
            Source = f.Source
        }).ToList()  
    };
 }
}
    