using System.Net;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Scanners;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class HttpsScannerTests
{
    private sealed class StubWebsiteSecurityClient
        : IWebsiteSecurityClient
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

    [Fact]
    public async Task ScanAsync_ShouldReturnInfoFinding_WhenHttpsConfigurationIsValid()
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

        var scanner = new HttpsScanner(websiteSecurityClient);

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
            "HTTPS yapılandırması uygun",
            findingList[0].Title);
        Assert.Equal(
            FindingSeverity.Info,
            findingList[0].Severity);
        Assert.Equal(
            0,
            findingList[0].ScoreImpact);
    }

    //http yok yönlendirme yok testi
    [Fact]
    public async Task ScanAsync_ShouldReturnFindings_WhenHttpsIsNotUsedAndRedirectIsMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "http://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: false,
            RedirectsToHttps: false,
            Headers: new Dictionary<string, string>());

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new HttpsScanner(websiteSecurityClient);

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
        Assert.Equal(2, findings.Count);

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "HTTPS kullanılmıyor" &&
                finding.Severity == FindingSeverity.High &&
                finding.ScoreImpact == 30);

        Assert.Contains(
            findings,
            finding =>
                finding.Title == "HTTP adresi HTTPS'e yönlenmiyor" &&
                finding.Severity == FindingSeverity.Medium);
    }

    //http var yönlendirme yok testi
    [Fact]
    public async Task ScanAsync_ShouldReturnRedirectFinding_WhenHttpsIsUsedButRedirectIsMissing()
    {
        // Arrange
        var securityResult = new WebsiteSecurityResult(
            OriginalUrl: "http://example.com",
            FinalUrl: "https://example.com",
            StatusCode: HttpStatusCode.OK,
            UsesHttps: true,
            RedirectsToHttps: false,
            Headers: new Dictionary<string, string>());

        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => Task.FromResult(securityResult));

        var scanner = new HttpsScanner(websiteSecurityClient);

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
            "HTTP adresi HTTPS'e yönlenmiyor",
            findingList[0].Title);
        Assert.Equal(
            FindingSeverity.Medium,
            findingList[0].Severity);
        Assert.Equal(
            10,
            findingList[0].ScoreImpact);
    }

    //timeout testi
    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenRequestTimesOut()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new TimeoutException(
                "Web sitesi zaman aşımına uğradı."));

        var scanner = new HttpsScanner(websiteSecurityClient);

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
            "Web sitesi kontrolü zaman aşımına uğradı",
            findingList[0].Title);
        Assert.Equal(
            FindingSeverity.Low,
            findingList[0].Severity);
        Assert.Equal(
            0,
            findingList[0].ScoreImpact);
    }

    // bağlantı-sertifika hatası testi
    [Fact]
    public async Task ScanAsync_ShouldReturnUnavailableFinding_WhenHttpRequestFails()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new HttpRequestException(
                "Bağlantı veya sertifika hatası oluştu."));

        var scanner = new HttpsScanner(websiteSecurityClient);

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
            "Web sitesine ulaşılamadı",
            findingList[0].Title);
        Assert.Equal(
            FindingSeverity.Low,
            findingList[0].Severity);
        Assert.Equal(
            0,
            findingList[0].ScoreImpact);
    }

    //kullanıcı iptali testi
    [Fact]
public async Task ScanAsync_ShouldPropagateCancellation_WhenCallerCancelsRequest()
{
       // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, cancellationToken) =>
                Task.FromCanceled<WebsiteSecurityResult>(
                    cancellationToken));

        var scanner = new HttpsScanner(websiteSecurityClient);

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

    //yanlış hedef türü testi
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

        var scanner = new HttpsScanner(websiteSecurityClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Email,
            TargetValue = "test@example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => scanner.ScanAsync(
                target,
                CancellationToken.None));
    }

    //null target testi
    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentNullException_WhenTargetIsNull()
    {
        // Arrange
        var websiteSecurityClient = new StubWebsiteSecurityClient(
            (_, _) => throw new InvalidOperationException(
                "Client çağrılmamalı."));

        var scanner = new HttpsScanner(websiteSecurityClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => scanner.ScanAsync(
                null!,
                CancellationToken.None));
    }
}