using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.GitHub;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using Moq;
using Xunit;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class GitHubExposureScannerTests
{
    [Fact]
    public async Task ScanAsync_RateLimitExceeded_ReturnsInfoFinding_DoesNotThrow()
    {
        // Arrange
        var mockClient = new Mock<IGitHubApiClient>();
        mockClient
            .Setup(c => c.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubRateLimitExceededException("rate limit aşıldı"));

        var scanner = new GitHubExposureScanner(mockClient.Object);

        // Act
        var findings = await scanner.ScanAsync("herhangi-bir-kullanici", CancellationToken.None);

        // Assert
        Assert.Single(findings);
        Assert.Equal("GitHub taraması geçici olarak yapılamadı", findings[0].Title);
    }
   [Fact]
public async Task ScanAsync_HttpRequestException_ReturnsInfoFinding_DoesNotThrow()
{
    // Arrange
    var mockClient = new Mock<IGitHubApiClient>();
    mockClient
        .Setup(c => c.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new HttpRequestException("bağlantı hatası"));

    var scanner = new GitHubExposureScanner(mockClient.Object);

    // Act
    var findings = await scanner.ScanAsync("herhangi-bir-kullanici", CancellationToken.None);

    // Assert
    Assert.Single(findings);
    Assert.Equal("GitHub taraması başarısız oldu", findings[0].Title);
}

[Fact]
public async Task ScanAsync_UserNotFound_ReturnsNotFoundFinding()
{
    // Arrange
    var mockClient = new Mock<IGitHubApiClient>();
    mockClient
        .Setup(c => c.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((GitHubUserResult?)null);

    var scanner = new GitHubExposureScanner(mockClient.Object);

    // Act
    var findings = await scanner.ScanAsync("olmayan-kullanici", CancellationToken.None);

    // Assert
    Assert.Single(findings);
    Assert.Equal("GitHub kullanıcısı bulunamadı", findings[0].Title);
}

[Fact]
public async Task ScanAsync_HighRepoCount_ReturnsMediumSeverityFinding()
{
    // Arrange
    var mockClient = new Mock<IGitHubApiClient>();
    mockClient
        .Setup(c => c.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GitHubUserResult
        {
            Login = "test-user",
            PublicRepos = 100,
            Followers = 10,
            Following = 5,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            HtmlUrl = "https://github.com/test-user"
        });

    var scanner = new GitHubExposureScanner(mockClient.Object);

    // Act
    var findings = await scanner.ScanAsync("test-user", CancellationToken.None);

    // Assert
    Assert.Contains(findings, f => f.Title == "Yüksek sayıda açık repository");
} 
}