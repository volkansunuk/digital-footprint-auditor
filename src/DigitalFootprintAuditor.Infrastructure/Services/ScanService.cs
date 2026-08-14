using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Application.Realtime;
using Microsoft.EntityFrameworkCore;

namespace DigitalFootprintAuditor.Infrastructure.Services;

public class ScanService : IScanService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IReadOnlyCollection<IScanner> _scanners;
    private readonly IRiskCalculator _riskCalculator;
    private readonly IScanProgressNotifier _progressNotifier;

    public ScanService(
        ApplicationDbContext dbContext,
        IEnumerable<IScanner> scanners,
        IRiskCalculator riskCalculator,
        IScanProgressNotifier progressNotifier)
    {
        _dbContext = dbContext;
        _scanners = scanners.ToList();
        _riskCalculator = riskCalculator;
        _progressNotifier = progressNotifier;
    }
    public async Task<ScanResponseDto> CreateScanAsync(
    CreateScanRequestDto request,
    CancellationToken cancellationToken)
    {
        var scanId = await PrepareScanAsync(
            request,
            cancellationToken);

        return await RunScanAsync(
            scanId,
            cancellationToken);
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
                .ToList(),
            BuildScannerStatuses(scan.Findings));
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
                        .ToList(),
                    BuildScannerStatuses(scan.Findings));
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

    public async Task<ScanResponseDto> RunScanAsync(
    Guid scanId,
    CancellationToken cancellationToken)
    {
        var scan = await _dbContext.Scans
            .FirstOrDefaultAsync(
                scan => scan.Id == scanId,
                cancellationToken);

        if (scan is null)
        {
            throw new InvalidOperationException(
                $"Scan with ID {scanId} was not found.");
        }

        var targets = await _dbContext.ScanTargets
            .Where(target => target.ScanId == scanId)
            .ToListAsync(cancellationToken);

        scan.Status = ScanStatus.Running;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var allFindings = new List<ScanFinding>();
        var hasScannerFailure = false;

        foreach (var target in targets)
        {
            var matchingScanners = _scanners
                .Where(scanner =>
                    scanner.SupportedTargetTypes.Contains(
                        target.TargetType))
                .ToList();

            foreach (var scanner in matchingScanners)
            {
                await _progressNotifier.NotifyAsync(
                    new ScannerProgressDto(
                        scan.Id,
                        scanner.GetType().Name,
                        ScannerProgressStatus.Pending),
                    cancellationToken);
            }

            var scannerTasks = matchingScanners
                .Select(scanner =>
                    ExecuteScannerSafelyAsync(
                        scanner,
                        target,
                        cancellationToken))
                .ToList();

            var scannerResults =
                await Task.WhenAll(scannerTasks);

            foreach (var scannerResult in scannerResults)
            {
                allFindings.AddRange(
                    scannerResult.Findings);

                if (scannerResult.HasFailed)
                {
                    hasScannerFailure = true;
                }
            }
        }

        if (allFindings.Count > 0)
        {
            _dbContext.ScanFindings.AddRange(
                allFindings);
        }

        scan.RiskScore =
            _riskCalculator.CalculateScore(
                allFindings);

        scan.RiskLevel =
            _riskCalculator.CalculateRiskLevel(
                scan.RiskScore);

        scan.Status = hasScannerFailure
            ? ScanStatus.PartiallyCompleted
            : ScanStatus.Completed;

        scan.CompletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var targetDtos = targets
            .Select(target =>
                new ScanTargetInputDto(
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
            targetDtos,
            allFindings
                .Select(MapFindingToDto)
                .ToList(),
            BuildScannerStatuses(allFindings));
    }
    public async Task<Guid> PrepareScanAsync(
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

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return scan.Id;
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
            finding.CreatedAt,
            GetRecommendation(finding));
    }

    private static string GetRecommendation(ScanFinding finding)
    {
        if (finding.Title.Contains(
            "SPF",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "Domain için uygun bir SPF kaydı yapılandırmayı değerlendirin.";
        }

        if (finding.Title.Contains(
            "DMARC",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "DMARC politikasını yapılandırarak e-posta sahteciliğine " +
                "karşı korumayı güçlendirin.";
        }

        if (finding.Title.Contains(
            "E-Posta",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "Herkese açık e-posta paylaşımının gerekli olup olmadığını " +
                "değerlendirin.";
        }

        if (finding.Title.Contains(
            "HTTPS",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "Web sitesinde HTTPS kullanımını etkinleştirmeyi ve " +
                "HTTP trafiğini HTTPS'e yönlendirmeyi değerlendirin.";
        }

        if (finding.Title.Contains(
            "HSTS",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "Strict-Transport-Security başlığını yapılandırmayı değerlendirin.";
        }

        if (finding.Title.Contains(
            "Content-Security-Policy",
            StringComparison.OrdinalIgnoreCase) ||
            finding.Title.Contains(
                "CSP",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Uygun bir Content-Security-Policy başlığı yapılandırmayı değerlendirin.";
        }

        if (finding.ScoreImpact == 0)
        {
            return
                "Bu bulgu bilgi amaçlıdır. Mevcut yapılandırmayı düzenli olarak " +
                "gözden geçirmeye devam edin.";
        }

        return
            "Bulguyu inceleyin ve ilgili güvenlik yapılandırmasını gözden geçirin.";
    }

    private async Task<ScannerExecutionResult> ExecuteScannerSafelyAsync(
    IScanner scanner,
    ScanTarget target,
    CancellationToken cancellationToken)
    {
        var scannerName = scanner.GetType().Name;

        await _progressNotifier.NotifyAsync(
            new ScannerProgressDto(
                target.ScanId,
                scannerName,
                ScannerProgressStatus.Running),
            cancellationToken);

        try
        {
            var findings = await scanner.ScanAsync(
                target,
                cancellationToken);

            await _progressNotifier.NotifyAsync(
                new ScannerProgressDto(
                    target.ScanId,
                    scannerName,
                    ScannerProgressStatus.Completed),
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
            await _progressNotifier.NotifyAsync(
                new ScannerProgressDto(
                    target.ScanId,
                    scannerName,
                    ScannerProgressStatus.Failed),
                cancellationToken);

            var failureFinding = CreateScannerFailureFinding(
                target.ScanId,
                scannerName);

            return new ScannerExecutionResult(
                new[] { failureFinding },
                HasFailed: true);
        }
    }

    private static IReadOnlyCollection<ScannerStatusDto> BuildScannerStatuses(
        IEnumerable<ScanFinding> findings)
    {
        var findingList = findings.ToList();

        var scannerNames = findingList
            .Select(finding => finding.ScannerName)
            .Where(scannerName =>
                !string.IsNullOrWhiteSpace(scannerName))
            .Distinct()
            .ToList();

        return scannerNames
            .Select(scannerName =>
            {
                var hasFailure = findingList.Any(finding =>
                    finding.ScannerName == scannerName &&
                    finding.Title.Contains(
                        "çalıştırılamadı",
                        StringComparison.OrdinalIgnoreCase));

                return new ScannerStatusDto(
                    scannerName,
                    IsSuccessful: !hasFailure);
            })
            .ToList();
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