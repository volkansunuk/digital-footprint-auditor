using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalFootprintAuditor.UnitTests.Services;

public class ScanOrchestratorTests
{
    [Fact]
    public async Task RunScannersAsync_OneScannerFails_OtherScannerContinuesAndScanIsPartial()
    {
        var failingScanner = new StubScanner(
            ScanTargetType.Domain,
            _ => throw new HttpRequestException("Bağlantı hatası"));
        var successfulScanner = new StubScanner(
            ScanTargetType.Domain,
            _ =>
            [
                new ScanFindingResult
                {
                    ScannerName = "SuccessfulScanner",
                    Title = "Başarılı bulgu",
                    Description = "Tarama devam etti.",
                    Severity = FindingSeverity.Info,
                    Source = "Test"
                }
            ]);
        var logger = new Mock<ILogger<ScanOrchestrator>>();
        var orchestrator = new ScanOrchestrator([failingScanner, successfulScanner], logger.Object);
        var targets = new List<ScanTarget>
        {
            new() { TargetType = ScanTargetType.Domain, TargetValue = "example.com" }
        };

        var result = await orchestrator.RunScannersAsync(targets, CancellationToken.None);

        Assert.True(result.HadUnexpectedFailures);
        Assert.Contains(result.Findings, finding => finding.ScannerName == "SuccessfulScanner");
        Assert.Contains(result.Findings, finding => finding.Source == "System");
    }

    [Fact]
    public async Task RunScannersAsync_TargetTypeDoesNotMatch_DoesNotRunScanner()
    {
        var scanner = new StubScanner(
            ScanTargetType.Email,
            _ => throw new InvalidOperationException("Bu scanner çalışmamalı."));
        var logger = new Mock<ILogger<ScanOrchestrator>>();
        var orchestrator = new ScanOrchestrator([scanner], logger.Object);
        var targets = new List<ScanTarget>
        {
            new() { TargetType = ScanTargetType.Domain, TargetValue = "example.com" }
        };

        var result = await orchestrator.RunScannersAsync(targets, CancellationToken.None);

        Assert.False(result.HadUnexpectedFailures);
        Assert.Empty(result.Findings);
    }

    private sealed class StubScanner : IFootprintScanner
    {
        private readonly Func<string, List<ScanFindingResult>> _scan;

        public StubScanner(ScanTargetType supportedTargetType, Func<string, List<ScanFindingResult>> scan)
        {
            SupportedTargetType = supportedTargetType;
            _scan = scan;
        }

        public ScanTargetType SupportedTargetType { get; }

        public Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
            => Task.FromResult(_scan(targetValue));
    }
}
