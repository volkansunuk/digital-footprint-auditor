using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Scanners;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class DnsSecurityScannerTests
{
    private sealed class StubDnsClient : IDnsClient
    {
        private readonly Func<
            CancellationToken,
            Task<DnsRecordResult>> _handler;

        public StubDnsClient(
            Func<CancellationToken, Task<DnsRecordResult>> handler)
        {
            _handler = handler;
        }

        public Task<DnsRecordResult> GetRecordsAsync(
            string domain,
            CancellationToken cancellationToken)
        {
            return _handler(cancellationToken);
        }
    }

    [Fact]
    public async Task ScanAsync_ShouldReportMissingSpfAndDmarcWhenTxtRecordsAreMissing()
    {
        var dnsClient = new StubDnsClient(
            _ => Task.FromResult(
                new DnsRecordResult(
                    ARecords:
                        new[] { "93.184.216.34" },
                    AaaaRecords:
                        new[]
                        {
                            "2606:2800:220:1:248:1893:25c8:1946"
                        },
                    MxRecords:
                        new[] { "mail.example.com" },
                    TxtRecords:
                        Array.Empty<string>(),
                    DmarcRecords:
                        Array.Empty<string>())));

        var scanner = new DnsSecurityScanner(dnsClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var spfFinding = Assert.Single(
            findings,
            finding =>
                finding.Title == "SPF kaydı bulunamadı");

        var dmarcFinding = Assert.Single(
            findings,
            finding =>
                finding.Title == "DMARC kaydı bulunamadı");

        Assert.Equal(
            FindingSeverity.Low,
            spfFinding.Severity);

        Assert.Equal(
            10,
            spfFinding.ScoreImpact);

        Assert.Contains(
            "10 puan",
            spfFinding.Description);

        Assert.Equal(
            FindingSeverity.Medium,
            dmarcFinding.Severity);

        Assert.Equal(
            15,
            dmarcFinding.ScoreImpact);

        Assert.Contains(
            "15 puan",
            dmarcFinding.Description);
    }

    [Fact]
    public async Task ScanAsync_ShouldNotReportSpfOrDmarc_WhenRecordsExist()
    {
        var scanner = new DnsSecurityScanner(
        new StubDnsClient(_ => Task.FromResult(
            new DnsRecordResult(
                ARecords: new[] { "1.1.1.1" },
                AaaaRecords: Array.Empty<string>(),
                MxRecords: new[] { "mail.example.com" },
                TxtRecords: new[] { "v=spf1 include:_spf.google.com ~all" },
                DmarcRecords: new[] { "v=DMARC1; p=reject" }))));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        Assert.DoesNotContain(findings, x => x.Title == "SPF kaydı bulunamadı");
        Assert.DoesNotContain(findings, x => x.Title == "DMARC kaydı bulunamadı");
    }

    [Fact]
    public async Task ScanAsync_ShouldReportOnlySpf_WhenOnlySpfIsMissing()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ => Task.FromResult(
                new DnsRecordResult(
                    ARecords: new[] { "1.1.1.1" },
                    AaaaRecords: Array.Empty<string>(),
                    MxRecords: new[] { "mail.example.com" },
                    TxtRecords: Array.Empty<string>(),
                    DmarcRecords: new[] { "v=DMARC1; p=reject" }))));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        Assert.Contains(findings, x => x.Title == "SPF kaydı bulunamadı");
        Assert.DoesNotContain(findings, x => x.Title == "DMARC kaydı bulunamadı");
    }

    [Fact]
    public async Task ScanAsync_ShouldReportOnlyDmarc_WhenOnlyDmarcIsMissing()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ => Task.FromResult(
                new DnsRecordResult(
                    ARecords: new[] { "1.1.1.1" },
                    AaaaRecords: Array.Empty<string>(),
                    MxRecords: new[] { "mail.example.com" },
                    TxtRecords: new[] { "v=spf1 include:_spf.google.com ~all" },
                    DmarcRecords: Array.Empty<string>()))));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        Assert.DoesNotContain(findings, x => x.Title == "SPF kaydı bulunamadı");
        Assert.Contains(findings, x => x.Title == "DMARC kaydı bulunamadı");
    }

    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentException_WhenTargetTypeIsInvalid()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ => Task.FromResult(
                new DnsRecordResult(
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>()))));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Email,
            TargetValue = "example.com"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            scanner.ScanAsync(target, CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenDnsTimesOut()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ =>
                throw new TimeoutException()));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        Assert.Contains(findings,
            x => x.Title == "DNS sorgusu zaman aşımına uğradı");
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnConnectionFinding_WhenHttpFails()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ =>
                throw new HttpRequestException()));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        Assert.Contains(findings,
            x => x.Title == "DNS servisine ulaşılamadı");
    }

    [Fact]
    public async Task ScanAsync_ShouldPropagateCancellation_WhenCancelled()
    {
        var scanner = new DnsSecurityScanner(
            new StubDnsClient(_ =>
                throw new OperationCanceledException()));

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scanner.ScanAsync(target, cts.Token));
    }
}