namespace DigitalFootprintAuditor.UnitTests.Scanners;

using System.Net;
using Moq;
using Moq.Protected;
using Xunit;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using DigitalFootprintAuditor.Domain.Enums;

public class GitHubProfileScannerTests
{
    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string jsonResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse)
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        return client;
    }

    // Test metotlarını buraya ekliyoruz ([Fact] veya [Theory])

    [Fact] //xUnit'e bunun çalıştırılabilir bir test metodu olduğunu bildirir.
    public async Task ScanAsync_ShouldReturnFindings_WhenUserExists()
    {
        var jsonResponse = "{\"login\":\"testuser\",\"public_repos\":5,\"email\":\"testuser@example.com\"}";
        HttpClient mockHttpClient = CreateMockHttpClient(HttpStatusCode.OK, jsonResponse);

        var scanner = new GitHubProfileScanner(mockHttpClient); // mockHttpClient kullanarak test edeceğimiz scanner sınıfından bir nesne üret:
        var findings = await scanner.ScanAsync("testuser", CancellationToken.None); // Scanner'ın ScanAsync metodunu çalıştırıp sonucu al:

        Assert.NotEmpty(findings);
        Assert.True(findings.Count() >= 2);
        Assert.Contains(findings, f => f.Title.Contains("Açık E-Posta"));
        
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnNotFoundFinding_WhenUserDoesNotExist()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient(HttpStatusCode.NotFound, string.Empty);
        var scanner = new GitHubProfileScanner(mockHttpClient);

        // Act
        var findings = await scanner.ScanAsync("nonexistentuser12345", CancellationToken.None);

        // Assert
        var findingList = findings.ToList();
        Assert.Single(findingList);
        Assert.Contains("Bulunamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Info, findingList[0].Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldHandleNetworkError_WhenHttpRequestFails()
    {
        // Arrange: Exception fırlatan özel bir HttpMessageHandler Mock'u
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Ağ bağlantısı kopuk"));

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var scanner = new GitHubProfileScanner(client);

        // Act
        var findings = await scanner.ScanAsync("anyuser", CancellationToken.None);

        // Assert
        var findingList = findings.ToList();
        Assert.Single(findingList);
        Assert.Contains("Ulaşılamadı", findingList[0].Title);
        Assert.Equal(FindingSeverity.Low, findingList[0].Severity);
    }
    
}