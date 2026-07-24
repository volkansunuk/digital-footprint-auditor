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

    public async Task<ScanResponseDto> CreateScanAsync(CreateScanRequestDto request)
    {
        var scan = new Scan
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = ScanStatus.Pending,   // Completed değil - henüz hiçbir scanner çalışmadı
            RiskScore = 0
        };

        var targets = request.Targets.Select(t => new ScanTarget
        {
            Id = Guid.NewGuid(),
            ScanId = scan.Id,
            TargetType = t.TargetType,
            TargetValue = t.TargetValue
        }).ToList();

        _dbContext.Scans.Add(scan);
        _dbContext.ScanTargets.AddRange(targets);

        await _dbContext.SaveChangesAsync();

        return new ScanResponseDto(
            scan.Id,
            scan.CreatedAt,
            scan.CompletedAt,
            scan.Status,
            scan.RiskScore,
            RiskLevel.Low,   // "Low" string'i değil, enum değeri
            request.Targets,
            new List<ScanFindingDto>()
        );
    }

    public async Task<ScanResponseDto?> GetScanByIdAsync(Guid id)
    {
        var scan = await _dbContext.Scans
            .Include(s => s.Findings)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (scan == null)
            return null;

        var scanTargets = await _dbContext.ScanTargets
            .Where(t => t.ScanId == id)
            .Select(t => new ScanTargetInputDto(t.TargetType, t.TargetValue))
            .ToListAsync();

        return new ScanResponseDto(
            scan.Id,
            scan.CreatedAt,
            scan.CompletedAt,
            scan.Status,
            scan.RiskScore,
            RiskLevel.Low,
            scanTargets,
            scan.Findings.Select(f => new ScanFindingDto(
                f.Id,
                f.ScannerName,
                f.Title,
                f.Description,
                f.Severity,
                f.ScoreImpact,
                f.Source,
                f.CreatedAt
            )).ToList()
        );
    }

    public async Task<IEnumerable<ScanResponseDto>> GetAllScansAsync()
    {
        var scans = await _dbContext.Scans
            .Include(s => s.Findings)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var scanIds = scans.Select(s => s.Id).ToList();

        var allTargets = await _dbContext.ScanTargets
            .Where(t => scanIds.Contains(t.ScanId))
            .ToListAsync();

        return scans.Select(scan =>
        {
            var targetsForScan = allTargets
                .Where(t => t.ScanId == scan.Id)
                .Select(t => new ScanTargetInputDto(t.TargetType, t.TargetValue))
                .ToList();

            return new ScanResponseDto(
                scan.Id,
                scan.CreatedAt,
                scan.CompletedAt,
                scan.Status,
                scan.RiskScore,
                RiskLevel.Low,
                targetsForScan,
                scan.Findings.Select(f => new ScanFindingDto(
                    f.Id,
                    f.ScannerName,
                    f.Title,
                    f.Description,
                    f.Severity,
                    f.ScoreImpact,
                    f.Source,
                    f.CreatedAt
                )).ToList()
            );
        });
    }

    public async Task<bool> DeleteScanAsync(Guid id)
    {
        var scan = await _dbContext.Scans.FindAsync(id);
        if (scan == null)
            return false;

        _dbContext.Scans.Remove(scan);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}