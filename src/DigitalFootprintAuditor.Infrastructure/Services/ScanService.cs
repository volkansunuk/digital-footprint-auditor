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

    public ScanService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return new ScanResponseDto(
            scan.Id,
            scan.CreatedAt,
            scan.CompletedAt,
            scan.Status,
            scan.RiskScore,
            scan.RiskLevel,
            request.Targets,
            Array.Empty<ScanFindingDto>());
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

    private static ScanFindingDto MapFindingToDto(ScanFinding finding)
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
}