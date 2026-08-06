using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalFootprintAuditor.Infrastructure.Services;

public class ScanService : IScanService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IReadOnlyCollection<IScanner> _scanners;
    private readonly IRiskCalculator _riskCalculator;

    public ScanService(
        ApplicationDbContext dbContext,
        IEnumerable<IScanner> scanners,
        IRiskCalculator riskCalculator)
    {
        _dbContext = dbContext;
        _scanners = scanners.ToList();
        _riskCalculator = riskCalculator;
    }

    public async Task<ScanResponseDto> CreateScanAsync(
        CreateScanRequestDto request,
        CancellationToken cancellationToken)
    {
        var scan = new Scan
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = ScanStatus.Pending,
            RiskScore = 0,
            RiskLevel = RiskLevel.Low
        };

        var targets = request.Targets
            .Select(target => new ScanTarget
            {
                Id = Guid.NewGuid(),
                ScanId = scan.Id,
                TargetType = target.TargetType,
                TargetValue = target.TargetValue
            })
            .ToList();

        _dbContext.Scans.Add(scan);
        _dbContext.ScanTargets.AddRange(targets);

        await _dbContext.SaveChangesAsync(cancellationToken);

        scan.Status = ScanStatus.Running;

        var allFindings = new List<ScanFinding>();

        var hasScannerFailure = false;

        foreach (var target in targets)
        {
            var matchingScanners = _scanners
                .Where(scanner =>
                    scanner.SupportedTargetTypes.Contains(
                        target.TargetType))
                .ToList();

            var scannerTasks = matchingScanners
                .Select(scanner =>
                    ExecuteScannerSafelyAsync(
                        scanner,
                        target,
                        cancellationToken))
                .ToList();

            var scannerResults = await Task.WhenAll(scannerTasks);

            foreach (var scannerResult in scannerResults)
            {
                allFindings.AddRange(scannerResult.Findings);

                if (scannerResult.HasFailed)
                {
                    hasScannerFailure = true;
                }
            }
        }

        if (allFindings.Count > 0)
        {
            _dbContext.ScanFindings.AddRange(allFindings);
        }

        scan.RiskScore =
            _riskCalculator.CalculateScore(allFindings);

        scan.RiskLevel =
            _riskCalculator.CalculateRiskLevel(
                scan.RiskScore);

        scan.Status = hasScannerFailure
            ? ScanStatus.PartiallyCompleted
            : ScanStatus.Completed;
        scan.CompletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);


        return new ScanResponseDto(
            scan.Id,
            scan.CreatedAt,
            scan.CompletedAt,
            scan.Status,
            scan.RiskScore,
            scan.RiskLevel,
            request.Targets,
            allFindings
                .Select(MapFindingToDto)
                .ToList());
    }

    public async Task<ScanResponseDto?> GetScanByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scan = await _dbContext.Scans
            .Include(scan => scan.Findings)
            .FirstOrDefaultAsync(
                scan => scan.Id == id,
                cancellationToken);

        if (scan is null)
        {
            return null;
        }

        var scanTargets = await _dbContext.ScanTargets
            .Where(target => target.ScanId == id)
            .Select(target => new ScanTargetInputDto(
                target.TargetType,
                target.TargetValue))
            .ToListAsync(cancellationToken);

        scan.RiskScore = _riskCalculator.CalculateScore(scan.Findings);
        scan.RiskLevel = _riskCalculator.CalculateRiskLevel(scan.RiskScore);

        return new ScanResponseDto(
            scan.Id,
            scan.CreatedAt,
            scan.CompletedAt,
            scan.Status,
            scan.RiskScore,
            scan.RiskLevel,
            scanTargets,
            scan.Findings
                .Select(MapFindingToDto)
                .ToList());
    }

    public async Task<IReadOnlyCollection<ScanResponseDto>> GetAllScansAsync(
        CancellationToken cancellationToken)
    {
        var scans = await _dbContext.Scans
            .Include(scan => scan.Findings)
            .OrderByDescending(scan => scan.CreatedAt)
            .ToListAsync(cancellationToken);

        var scanIds = scans
            .Select(scan => scan.Id)
            .ToList();

        var allTargets = await _dbContext.ScanTargets
            .Where(target => scanIds.Contains(target.ScanId))
            .ToListAsync(cancellationToken);


        return scans
            .Select(scan =>
            {
                var targetsForScan = allTargets
                    .Where(target => target.ScanId == scan.Id)
                    .Select(target => new ScanTargetInputDto(
                        target.TargetType,
                        target.TargetValue))
                    .ToList();

                scan.RiskScore = _riskCalculator.CalculateScore(scan.Findings);
                scan.RiskLevel = _riskCalculator.CalculateRiskLevel(scan.RiskScore);

                return new ScanResponseDto(
                    scan.Id,
                    scan.CreatedAt,
                    scan.CompletedAt,
                    scan.Status,
                    scan.RiskScore,
                    scan.RiskLevel,
                    targetsForScan,
                    scan.Findings
                        .Select(MapFindingToDto)
                        .ToList());
            })
            .ToList();
    }

    public async Task<bool> DeleteScanAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var scan = await _dbContext.Scans.FindAsync(
            [id],
            cancellationToken);

        if (scan is null)
        {
            return false;
        }

        _dbContext.Scans.Remove(scan);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private ScanFindingDto MapFindingToDto(ScanFinding finding)
    {
        return new ScanFindingDto(
            finding.Id,
            finding.ScannerName,
            finding.Title,
            finding.Description,
            finding.Severity,
            finding.ScoreImpact,
            finding.Source,
            finding.CreatedAt);
    }

    private async Task<ScannerExecutionResult> ExecuteScannerSafelyAsync(
        IScanner scanner,
        ScanTarget target,
        CancellationToken cancellationToken)
    {
        try
        {
            var findings = await scanner.ScanAsync(
                target,
                cancellationToken);

            return new ScannerExecutionResult(
                findings,
                HasFailed: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            var failureFinding = CreateScannerFailureFinding(
                target.ScanId,
                scanner.GetType().Name);

            return new ScannerExecutionResult(
                new[] { failureFinding },
                HasFailed: true);
        }
    }

    private static ScanFinding CreateScannerFailureFinding(
        Guid scanId,
        string scannerName)
    {
        return new ScanFinding
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            ScannerName = scannerName,
            Title = $"{scannerName} çalıştırılamadı",
            Description =
                "Scanner çalışırken beklenmeyen bir hata oluştu. " +
                "Diğer scanner işlemlerine devam edildi.",
            Severity = FindingSeverity.Low,
            ScoreImpact = 0,
            Source = "Scanner Orchestration",
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed record ScannerExecutionResult(
        IReadOnlyCollection<ScanFinding> Findings,
        bool HasFailed);
}