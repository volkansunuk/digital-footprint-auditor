using System.Net;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using Moq;
using Moq.Protected;
using Xunit;

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
        // Arrange
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

        var mockHttpClient = CreateMockHttpClient(
            HttpStatusCode.OK,
            jsonResponse);

        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

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
        Assert.NotEmpty(findings);
        Assert.True(findings.Count >= 2);

        Assert.Contains(
            findings,
            finding => finding.Title.Contains("E-Posta"));

        Assert.All(
            findings,
            finding => Assert.Equal(target.ScanId, finding.ScanId));
    }

    [Fact(Skip = "Gün 8: 404 hata yönetimi eklendikten sonra tamamlanacak.")]
    public async Task ScanAsync_ShouldReturnNotFoundFinding_WhenUserDoesNotExist()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient(
            HttpStatusCode.NotFound,
            string.Empty);

        var gitHubClient = new GitHubClient(mockHttpClient);
        var scanner = new GitHubProfileScanner(gitHubClient);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "nonexistentuser12345"
        };

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Bulunamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Info, findingList[0].Severity);
    }

    [Fact(Skip = "Gün 8: Ağ hatası yönetimi eklendikten sonra tamamlanacak.")]
    public async Task ScanAsync_ShouldHandleNetworkError_WhenHttpRequestFails()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(
                new HttpRequestException("Ağ bağlantısı kopuk"));

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

        // Act
        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        // Assert
        var findingList = findings.ToList();

        Assert.Single(findingList);
        Assert.Contains("Ulaşılamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }
}