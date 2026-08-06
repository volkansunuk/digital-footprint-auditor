using System.Net;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using Moq;
using Moq.Protected;


namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class GitHubProfileScannerTests
{
    private static HttpClient CreateMockHttpClient(
        HttpStatusCode statusCode,
        string jsonResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnFindings_WhenUserExists()
    {
        const string jsonResponse = """
        {
          "login": "testuser",
          "public_repos": 5,
          "email": "testuser@example.com",
          "bio": "Test kullanıcısı",
          "created_at": "2020-01-01T00:00:00Z",
          "updated_at": "2025-01-01T00:00:00Z"
        }
        """;

        var mockHttpClient = CreateMockHttpClient(HttpStatusCode.OK, jsonResponse);
        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);

        // Assert
        Assert.NotEmpty(findings);
        Assert.True(findings.Count >= 2);

        var emailFinding = Assert.Single(
            findings, 
            finding =>
                finding.Title ==
                "Herkese Açık E-Posta Adresi Bulundu");

        Assert.Equal(
            FindingSeverity.Medium,
            emailFinding.Severity);

        Assert.Equal(
            10,
            emailFinding.ScoreImpact);

        Assert.Contains(
            "10 puan",
            emailFinding.Description);

        Assert.All(
            findings,
            finding =>
                Assert.Equal(
                    target.ScanId,
                    finding.ScanId));

        Assert.All(
            findings.Where(finding =>
                finding.Title !=
                "Herkese Açık E-Posta Adresi Bulundu"),
            finding =>
                Assert.Equal(
                    0,
                    finding.ScoreImpact));
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnNotFoundFinding_WhenUserDoesNotExist()
    {
        var mockHttpClient = CreateMockHttpClient(HttpStatusCode.NotFound, string.Empty);
        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "nonexistentuser12345"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Bulunamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Info, findingList[0].Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldHandleNetworkError_WhenHttpRequestFails()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Ağ bağlantısı kopuk"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "anyuser"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Ulaşılamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnUnavailableFinding_WhenRateLimitIsExceeded()
    {
        var mockHttpClient = CreateMockHttpClient(HttpStatusCode.Forbidden, string.Empty);
        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Ulaşılamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenRequestTimesOut()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "anyuser"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Zaman Aşımına", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldPropagateCancellation_WhenCallerCancelsRequest()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scanner.ScanAsync(target, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnUnavailableFinding_WhenServerReturnsError()
    {
        var mockHttpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, string.Empty);
        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        var findings = await scanner.ScanAsync(target, CancellationToken.None);
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Ulaşılamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }
}