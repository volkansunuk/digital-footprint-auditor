using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Services;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Application.Realtime;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalFootprintAuditor.UnitTests.Services;

public class ScanServiceTests
{
    private static ApplicationDbContext CreateDbContext() 
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class StubScanner : IScanner
    {
        private readonly Func<
                ScanTarget,
                CancellationToken,
                Task<IReadOnlyCollection<ScanFinding>>> _handler;

            public StubScanner(
                TargetType supportedTargetType,
                Func<
                    ScanTarget,
                    CancellationToken,
                    Task<IReadOnlyCollection<ScanFinding>>> handler)
            {
                SupportedTargetTypes =
                    new[] { supportedTargetType };

                _handler = handler;
            }

            public IReadOnlyCollection<TargetType>
                SupportedTargetTypes { get; }

            public Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
                ScanTarget target,
                CancellationToken cancellationToken)
            {
                return _handler(target, cancellationToken);
            }
    }

    private sealed class StubProgressNotifier : IScanProgressNotifier
{
    public List<ScannerProgressDto> Notifications { get; } = new();

    public Task NotifyAsync(
        ScannerProgressDto progress,
        CancellationToken cancellationToken)
    {
        Notifications.Add(progress);

        return Task.CompletedTask;
    }
}

    [Fact]
    public async Task CreateScanAsync_ShouldCombineFindings_FromMatchingScanners()
    {
        await using var dbContext = CreateDbContext();

        var firstScanner = new StubScanner(
            TargetType.Domain,
                (target, _) => Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "FirstScanner",
                            Title = "Birinci bulgu",
                            Description = "Birinci scanner tarafından üretildi.",
                            Severity = FindingSeverity.Low,
                            ScoreImpact = 5,
                            Source = "Test",
                            CreatedAt = DateTime.UtcNow
                        }
                    }));

        var secondScanner = new StubScanner(
        TargetType.Domain,
        (target, _) => Task.FromResult<IReadOnlyCollection<ScanFinding>>(
            new[]
            {
                new ScanFinding
                {
                    Id = Guid.NewGuid(),
                    ScanId = target.ScanId,
                    ScannerName = "SecondScanner",
                    Title = "İkinci bulgu",
                    Description = "İkinci scanner tarafından üretildi.",
                    Severity = FindingSeverity.Medium,
                    ScoreImpact = 10,
                    Source = "Test",
                    CreatedAt = DateTime.UtcNow
                }
            }));

            var service = new ScanService(
                dbContext,
                new IScanner[]
                {
                    firstScanner,
                    secondScanner
                },
                new RiskScoringService(),
                new StubProgressNotifier());

            var request = new CreateScanRequestDto(
                new[]
                {
                    new ScanTargetInputDto(
                        TargetType.Domain,
                        "example.com")
                });

            // Act
            var result = await service.CreateScanAsync(
                request,
                CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Findings.Count);

            Assert.Contains(
                result.Findings,
                finding => finding.Title == "Birinci bulgu");

            Assert.Contains(
                result.Findings,
                finding => finding.Title == "İkinci bulgu");

            Assert.Equal(15, result.RiskScore);
            Assert.Equal(RiskLevel.Low, result.RiskLevel);
            Assert.Equal(ScanStatus.Completed, result.Status);
            Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldExecuteMatchingScannersInParallel()
    {
        //arrange
        await using var dbContext = CreateDbContext();

        var startedScannerCount = 0;

        var bothScannersStarted = 
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);    
            
        async Task<IReadOnlyCollection<ScanFinding>> ScannerHandler(
            ScanTarget target,
            CancellationToken cancellationToken)
        {
            var currentCount =
                Interlocked.Increment(
                    ref startedScannerCount);

            if (currentCount == 2)
            {
                bothScannersStarted.TrySetResult(true);
            }

            await bothScannersStarted.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken);

            return new[]
            {
                new ScanFinding
                {
                    Id = Guid.NewGuid(),
                    ScanId = target.ScanId,
                    ScannerName = "ParallelScanner",
                    Title = "Paralel çalışma bulgusu",
                    Description =
                        "Scanner paralel olarak çalıştırıldı.",
                    Severity = FindingSeverity.Info,
                    ScoreImpact = 0,
                    Source = "Test",
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        var firstScanner = new StubScanner(
            TargetType.Domain,
            ScannerHandler);

        var secondScanner = new StubScanner(
            TargetType.Domain,
            ScannerHandler);

        var service = new ScanService(
            dbContext,
            new IScanner[]
            {
                firstScanner,
                secondScanner
            },
            new RiskScoringService(),
            new StubProgressNotifier());

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        // Act
        var result = await service.CreateScanAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(
            2,
            startedScannerCount);

        Assert.Equal(
            2,
            result.Findings.Count);

        Assert.Equal(
            ScanStatus.Completed,
            result.Status);
    }
    

    //bir scanner başarısız olduğunda
    [Fact]
    public async Task CreateScanAsync_ShouldReturnPartiallyCompleted_WhenOneScannerFails()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var successfulScannerOne = new StubScanner(
            TargetType.Domain,
            (target, _) =>
                Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "SuccessfulScannerOne",
                            Title = "Birinci başarılı bulgu",
                            Description =
                                "Birinci scanner başarıyla çalıştı.",
                            Severity = FindingSeverity.Low,
                            ScoreImpact = 5,
                            Source = "Test",
                            CreatedAt = DateTime.UtcNow
                        }
                    }));

        var failingScanner = new StubScanner(
            TargetType.Domain,
            (_, _) => throw new InvalidOperationException(
                "Scanner beklenmeyen bir hata verdi."));

        var successfulScannerTwo = new StubScanner(
            TargetType.Domain,
            (target, _) =>
                Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "SuccessfulScannerTwo",
                            Title = "İkinci başarılı bulgu",
                            Description =
                                "İkinci scanner başarıyla çalıştı.",
                            Severity = FindingSeverity.Medium,
                            ScoreImpact = 10,
                            Source = "Test",
                            CreatedAt = DateTime.UtcNow
                        }
                    }));

        var service = new ScanService(
            dbContext,
            new IScanner[]
            {
                successfulScannerOne,
                failingScanner,
                successfulScannerTwo
            },
            new RiskScoringService(),
            new StubProgressNotifier());

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        // Act
        var result = await service.CreateScanAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Findings.Count);

        Assert.Contains(
            result.Findings,
            finding =>
                finding.Title == "Birinci başarılı bulgu");

        Assert.Contains(
            result.Findings,
            finding =>
                finding.Title == "İkinci başarılı bulgu");

        Assert.Contains(
            result.Findings,
            finding =>
                finding.Title.Contains("StubScanner") &&
                finding.Title.Contains("çalıştırılamadı"));

        Assert.Equal(15, result.RiskScore);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.Equal(ScanStatus.PartiallyCompleted, result.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldRunOnlyScannersMatchingTargetType()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var websiteScannerCallCount = 0;
        var domainScannerCallCount = 0;

        var websiteScanner = new StubScanner(
            TargetType.Website,
            (target, _) =>
            {
                websiteScannerCallCount++;

                return Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "WebsiteScanner",
                            Title = "Website bulgusu",
                            Description =
                                "Website scanner tarafından üretildi.",
                            Severity = FindingSeverity.Info,
                            ScoreImpact = 0,
                            Source = "Test",
                            CreatedAt = DateTime.UtcNow
                        }
                    });
            });

        var domainScanner = new StubScanner(
            TargetType.Domain,
            (_, _) =>
            {
                domainScannerCallCount++;

                return Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    Array.Empty<ScanFinding>());
            });

        var service = new ScanService(
            dbContext,
            new IScanner[]
            {
                websiteScanner,
                domainScanner
            },
            new RiskScoringService(),
            new StubProgressNotifier());

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Website,
                    "example.com")
            });

        // Act
        var result = await service.CreateScanAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, websiteScannerCallCount);
        Assert.Equal(0, domainScannerCallCount);

        var finding = Assert.Single(result.Findings);

        Assert.Equal(
            "Website bulgusu",
            finding.Title);

        Assert.Equal(
            ScanStatus.Completed,
            result.Status);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldMapRecommendationToFindingDto()
    {
        await using var dbContext = CreateDbContext();

        var scanner = new StubScanner(
            TargetType.Domain,
            (target, _) =>
                Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "DnsSecurityScanner",
                            Title = "DMARC kaydı bulunamadı",
                            Description = "Domain için DMARC kaydı bulunamadı.",
                            Severity = FindingSeverity.Medium,
                            ScoreImpact = 15,
                            Source = "DNS",
                            CreatedAt = DateTime.UtcNow
                        }
                    }));

        var service = new ScanService(
            dbContext,
            new IScanner[] { scanner },
            new RiskScoringService(),
            new StubProgressNotifier());

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        var result = await service.CreateScanAsync(
            request,
            CancellationToken.None);

        var finding = Assert.Single(result.Findings);

        Assert.False(
            string.IsNullOrWhiteSpace(finding.Recommendation));

        Assert.Contains(
            "DMARC",
            finding.Recommendation,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldReturnScannerStatuses_ForSuccessfulAndFailedScanners()
    {
        await using var dbContext = CreateDbContext();

        var successfulScanner = new StubScanner(
            TargetType.Domain,
            (target, _) =>
                Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    new[]
                    {
                        new ScanFinding
                        {
                            Id = Guid.NewGuid(),
                            ScanId = target.ScanId,
                            ScannerName = "SuccessfulScanner",
                            Title = "Başarılı bulgu",
                            Description = "Scanner başarıyla çalıştı.",
                            Severity = FindingSeverity.Info,
                            ScoreImpact = 0,
                            Source = "Test",
                            CreatedAt = DateTime.UtcNow
                        }
                    }));

        var failingScanner = new StubScanner(
            TargetType.Domain,
            (_, _) => throw new InvalidOperationException(
                "Test scanner hatası."));

        var service = new ScanService(
            dbContext,
            new IScanner[]
            {
                successfulScanner,
                failingScanner
            },
            new RiskScoringService(),
            new StubProgressNotifier());

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        var result = await service.CreateScanAsync(
            request,
            CancellationToken.None);

        Assert.Contains(
            result.ScannerStatuses,
            status =>
                status.ScannerName == "SuccessfulScanner" &&
                status.IsSuccessful);

        Assert.Contains(
            result.ScannerStatuses,
            status =>
                status.ScannerName == nameof(StubScanner) &&
                !status.IsSuccessful);

        Assert.Equal(
            ScanStatus.PartiallyCompleted,
            result.Status);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldNotifyProgress_AsPendingRunningCompleted()
    {
        await using var dbContext = CreateDbContext();

        var progressNotifier = new StubProgressNotifier();

        var scanner = new StubScanner(
            TargetType.Domain,
            (target, _) =>
                Task.FromResult<IReadOnlyCollection<ScanFinding>>(
                    Array.Empty<ScanFinding>()));

        var service = new ScanService(
            dbContext,
            new IScanner[] { scanner },
            new RiskScoringService(),
            progressNotifier);

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        await service.CreateScanAsync(
            request,
            CancellationToken.None);

        Assert.Equal(3, progressNotifier.Notifications.Count);

        Assert.Equal(
            ScannerProgressStatus.Pending,
            progressNotifier.Notifications[0].Status);

        Assert.Equal(
            ScannerProgressStatus.Running,
            progressNotifier.Notifications[1].Status);

        Assert.Equal(
            ScannerProgressStatus.Completed,
            progressNotifier.Notifications[2].Status);
    }

    [Fact]
    public async Task CreateScanAsync_ShouldNotifyProgress_AsPendingRunningFailed_WhenScannerFails()
    {
        await using var dbContext = CreateDbContext();

        var progressNotifier = new StubProgressNotifier();

        var scanner = new StubScanner(
            TargetType.Domain,
            (_, _) => throw new InvalidOperationException(
                "Scanner test hatası."));

        var service = new ScanService(
            dbContext,
            new IScanner[] { scanner },
            new RiskScoringService(),
            progressNotifier);

        var request = new CreateScanRequestDto(
            new[]
            {
                new ScanTargetInputDto(
                    TargetType.Domain,
                    "example.com")
            });

        await service.CreateScanAsync(
            request,
            CancellationToken.None);

        Assert.Equal(3, progressNotifier.Notifications.Count);

        Assert.Equal(
            ScannerProgressStatus.Pending,
            progressNotifier.Notifications[0].Status);

        Assert.Equal(
            ScannerProgressStatus.Running,
            progressNotifier.Notifications[1].Status);

        Assert.Equal(
            ScannerProgressStatus.Failed,
            progressNotifier.Notifications[2].Status);
    }
}