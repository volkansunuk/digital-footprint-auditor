using System.Net;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using Moq;
using Moq.Protected;


namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class GitHubRepositoryScannerTests
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
    private static HttpClient CreateThrowingHttpClient(
        Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
    }

    //başarılı
    [Fact]
    public async Task ScanAsync_ShouldReturnRepositorySummary_WhenRepositoriesExists()
    {
        //arrange
        const string jsonResponse = """
        [
            {
                "name": "first-repository",
                "description": "İlk repository.",
                "language": "c#",
                "fork": false,
                "archived": false,
                "created_at": "2024-01-01T00:00:00Z",
                "updated_at": "2025-01-01T00:00:00Z"
            },
            {
                "name": "second-repository",
                "description": "İkinci repository",
                "language": "JavaScript",
                "fork": true,
                "archived": true,
                "created_at": "2024-02-01T00:00:00Z",
                "updated_at": "2026-01-01T00:00:00Z"
            }
        ]
        """;
                
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK,
            jsonResponse);

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "GitHub Public Repository Özeti",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Info,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);

        Assert.Equal(
            nameof(GitHubRepositoryScanner),
            finding.ScannerName);

        Assert.Contains(
            "Toplam public repository sayısı: 2",
            finding.Description);

        Assert.Contains(
            "Fork repository sayısı: 1",
            finding.Description);

        Assert.Contains(
            "Arşivlenmiş repository sayısı: 1",
            finding.Description);

        Assert.Contains(
            "second-repository",
            finding.Description);

        Assert.Contains(
            "risk puanını artırmaz",
            finding.Description);
    
    }

    //Kullanıcı bulunamadı
    [Fact]
    public async Task ScanAsync_ShouldReturnUserNotFoundFinding_WhenUserDoesNotExist()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.NotFound,
            string.Empty);

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "missing-user"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "GitHub Kullanıcısı Bulunamadı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Info,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);

        Assert.Contains(
            "risk puanını artırmaz",
            finding.Description);
    }

    //boş repository listesi
    [Fact]
    public async Task ScanAsync_ShouldReturnNoRepositoriesFinding_WhenRepositoryListIsEmpty()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK,
            "[]");

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "Herkese Açık Repository Bulunamadı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Info,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);
    }

    //timeout testi
    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenRequestTimesOut()
    {
        // Arrange
        var httpClient = CreateThrowingHttpClient(
            new TaskCanceledException("Timeout"));

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "GitHub API Zaman Aşımına Uğradı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);
    }

    //http/rate limit testi
    [Fact]
    public async Task ScanAsync_ShouldReturnUnavailableFinding_WhenRequestFails()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.Forbidden,
            string.Empty);

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "testuser"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var finding = Assert.Single(findings);

        Assert.Equal(
            "GitHub Repository Bilgilerine Ulaşılamadı",
            finding.Title);

        Assert.Equal(
            FindingSeverity.Low,
            finding.Severity);

        Assert.Equal(
            0,
            finding.ScoreImpact);
    }

    //yanlış hedef türü testi
    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentException_WhenTargetTypeIsInvalid()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK,
            "[]");

        var gitHubClient = new GitHubClient(httpClient);
        var scanner = new GitHubRepositoryScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Domain,
            TargetValue = "example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            scanner.ScanAsync(
                target,
                CancellationToken.None));
    }
}