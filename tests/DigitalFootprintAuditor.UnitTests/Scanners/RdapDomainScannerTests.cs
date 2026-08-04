using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Scanners;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class RdapDomainScannerTests
{
    private sealed class StubRdapClient : IRdapClient
    {
        private readonly Func<CancellationToken, Task<RdapDomainResult?>> _handler;

        public StubRdapClient(Func<CancellationToken, Task<RdapDomainResult?>> handler)
        {
            _handler = handler;
        }

        public Task<RdapDomainResult?> GetDomainAsync(string domain, CancellationToken cancellationToken)
        {
            return _handler(cancellationToken);
        }
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("EXAMPLE.COM", "example.com")]
    [InlineData(" https://example.com ", "example.com")]
    [InlineData("https://example.com/about", "example.com")]
    [InlineData("http://example.com?test=1", "example.com")]
    public void NormalizeDomain_ShouldReturnHostOnly(
        string input,
        string expected)
    {
        var result = RdapDomainScanner.NormalizeDomain(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a valid domain")]
    public void NormalizeDomain_ShouldThrow_WhenInputIsInvalid(
        string input)
    {
        Assert.Throws<ArgumentException>(() =>
            RdapDomainScanner.NormalizeDomain(input));
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenClientTimesOut()
    {
        var scanner = new RdapDomainScanner(new StubRdapClient(_ => throw new TimeoutException()));
        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        var finding = Assert.Single(findings);
        Assert.Equal("RDAP İsteği Zaman Aşımına Uğradı", finding.Title);
        Assert.Equal(FindingSeverity.Low, finding.Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnConnectionErrorFinding_WhenClientThrowsHttpRequestException()
    {
        var scanner = new RdapDomainScanner(new StubRdapClient(_ => throw new HttpRequestException("connection failed")));
        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        var finding = Assert.Single(findings);
        Assert.Equal("RDAP Servisine Ulaşılamadı", finding.Title);
        Assert.Equal(FindingSeverity.Low, finding.Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnCancellationFinding_WhenClientThrowsTaskCanceledException()
    {
        var scanner = new RdapDomainScanner(new StubRdapClient(_ => throw new TaskCanceledException()));
        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        var finding = Assert.Single(findings);
        Assert.Equal("RDAP İsteği İptal Edildi", finding.Title);
        Assert.Equal(FindingSeverity.Low, finding.Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldDescribeMissingRegistrarAndNameserversSafely()
    {
        var scanner = new RdapDomainScanner(new StubRdapClient(_ => Task.FromResult<RdapDomainResult?>(new RdapDomainResult(
            Domain: "example.com",
            RegistrationDate: null,
            UpdatedDate: null,
            ExpirationDate: null,
            Registrar: null,
            Nameservers: Array.Empty<string>()))));
        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        var finding = Assert.Single(findings);
        Assert.Contains("Registrar: bilinmiyor.", finding.Description);
        Assert.Contains("Nameserver kaydı bulunamadı.", finding.Description);
    }
}