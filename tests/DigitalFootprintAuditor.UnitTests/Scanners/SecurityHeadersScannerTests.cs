using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Application.Abstractions;
using System.Net;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Infrastructure.Scanners;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class SecurityHeadersScannerTests
{
    private sealed class StubWebsiteSecurityClient : IWebsiteSecurityClient
    {
        private readonly Func<
            string,
            CancellationToken,
            Task<WebsiteSecurityResult>> _handler;

        public StubWebsiteSecurityClient(
            Func<string, CancellationToken, Task<WebsiteSecurityResult>> handler)
        {
            _handler = handler;
        }

        public Task<WebsiteSecurityResult> GetSecurityInfoAsync(
            string domain,
            CancellationToken cancellationToken)
        {
            return _handler(domain, cancellationToken);
        }
    }

    //tüm headerlar var
    [Fact]
    public async Task ScanAsync_ShouldReturnInfoFinding_WhenAllSecurityHeadersExist()
    {
         // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: true,
            Headers: new Dictionary<string, string>
            {
                ["Strict-Transport-Security"] = "max-age=31536000",
                ["Content-Security-Policy"] = "default-src 'self'",
                ["X-Content-Type-Options"] = "nosniff",
                ["Referrer-Policy"] = "strict-origin-when-cross-origin",
                ["Permissions-Policy"] = "camera=(), microphone=()"
            });

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Equal(
            "Temel güvenlik başlıkları bulundu",
            findingList[0].Title);
        Assert.Equal(
            FindingSeverity.Info,
            findingList[0].Severity);
        Assert.Equal(
            0,
            findingList[0].ScoreImpact);
    }
    
    //
    [Fact]
    public async Task ScanAsync_ShouldReturnFindings_WhenSomeSecurityHeadersAreMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: true,
            Headers: new Dictionary<string, string>
            {
                ["Strict-Transport-Security"] = "max-age=31536000"
                // Diğer başlıklar eksik
            });

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var findingList = findings.ToList();

        Assert.Equal(4, findingList.Count);
        Assert.Contains(
            findingList,
            finding =>
                finding.Title == "Content-Security-Policy başlığı bulunamadı");
        Assert.Contains(
            findingList,
            finding =>
                finding.Title == "X-Content-Type-Options başlığı bulunamadı");
        Assert.Contains(
            findingList,
            finding =>
                finding.Title == "Referrer-Policy başlığı bulunamadı");
        Assert.Contains(
            findingList,
            finding =>
                finding.Title == "Permissions-Policy başlığı bulunamadı");
    }

        // Tüm güvenlik başlıkları eksik
    [Fact]
    public async Task ScanAsync_ShouldReturnFiveFindings_WhenAllSecurityHeadersAreMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: true,
            Headers: new Dictionary<string, string>());

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        Assert.Equal(5, findings.Count);

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "HSTS başlığı bulunamadı");

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "Content-Security-Policy başlığı bulunamadı");

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "X-Content-Type-Options başlığı bulunamadı");

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "Referrer-Policy başlığı bulunamadı");

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "Permissions-Policy başlığı bulunamadı");
    }

    // Yalnızca HSTS eksik
    [Fact]
    public async Task ScanAsync_ShouldReturnHstsFinding_WhenOnlyHstsIsMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: true,
            Headers: new Dictionary<string, string>
            {
                ["Content-Security-Policy"] = "default-src 'self'",
                ["X-Content-Type-Options"] = "nosniff",
                ["Referrer-Policy"] = "strict-origin-when-cross-origin",
                ["Permissions-Policy"] = "camera=(), microphone=()"
            });

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "HSTS başlığı bulunamadı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            5,
            finding.ScoreImpact);
    }

    // Yalnızca CSP eksik
    [Fact]
    public async Task ScanAsync_ShouldReturnCspFinding_WhenOnlyCspIsMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: true,
            Headers: new Dictionary<string, string>
            {
                ["Strict-Transport-Security"] = "max-age=31536000",
                ["X-Content-Type-Options"] = "nosniff",
                ["Referrer-Policy"] = "strict-origin-when-cross-origin",
                ["Permissions-Policy"] = "camera=(), microphone=()"
            });

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "Content-Security-Policy başlığı bulunamadı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            5,
            finding.ScoreImpact);
    }

    // Timeout testi
    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenRequestTimesOut()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new TimeoutException(
                "Web sitesi zaman aşımına uğradı."));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "Güvenlik başlıkları kontrolü zaman aşımına uğradı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);
    }

    // Bağlantı veya sertifika hatası testi
    [Fact]
    public async Task ScanAsync_ShouldReturnUnavailableFinding_WhenHttpRequestFails()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new HttpRequestException(
                "Bağlantı veya sertifika hatası oluştu."));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "Web sitesinin güvenlik başlıkları kontrol edilemedi",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);
    }

    // Kullanıcı iptali testi
    [Fact]
    public async Task ScanAsync_ShouldPropagateCancellation_WhenCallerCancelsRequest()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, cancellationToken) =>
                Task.FromCanceled<WebsiteSecurityResult>(
                    cancellationToken));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Website,
            TargetValue = "example.com"
        };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scanner.ScanAsync(
                target,
                cancellationTokenSource.Token));
    }

    // Yanlış hedef türü testi
    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentException_WhenTargetTypeIsInvalid()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(
                new WebsiteSecurityResult(
                    OriginalUrl: "http://example.com",
                    FinalUrl: "https://example.com",
                    StatusCode: HttpStatusCode.OK,
                    UsesHttps: true,
                    RedirectsToHttps: true,
                    Headers: new Dictionary<string, string>())));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => scanner.ScanAsync(
                target,
                CancellationToken.None));
    }

    // Null target testi
    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentNullException_WhenTargetIsNull()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new InvalidOperationException(
                "Client çağrılmamalı."));

        var scanner = new SecurityHeadersScanner(
            websiteSecurityClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => scanner.ScanAsync(
                null!,
                CancellationToken.None));
    }

}