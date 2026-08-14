using System.Net;
using DigitalFootprintAuditor.Infrastructure.Gravatar;
using Moq;
using Moq.Protected;

namespace DigitalFootprintAuditor.UnitTests.Gravatar;

public class GravatarClientTests
{
    //
    [Fact]
    public async Task GetProfileAsync_ShouldReturnMappedResult_WhenProfileExists()
    {
        const string hash = "abc123";

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri!.AbsolutePath
                        .EndsWith($"/profiles/{hash}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "profile_url": "https://gravatar.com/example",
                        "display_name": "Example User",
                        "preferred_username": "example"
                    }
                    """)
            });

        using var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.gravatar.com/v3/")
        };

        var client = new GravatarClient(httpClient);

        var result = await client.GetProfileAsync(
            hash,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            "https://gravatar.com/example",
            result.ProfileUrl);
        Assert.Equal("Example User", result.DisplayName);
        Assert.Equal("example", result.PreferredUsername);
    }

    //
    [Fact]
    public async Task GetProfileAsync_ShouldReturnNull_WhenProfileIsNotFound()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(
                HttpStatusCode.NotFound));

        using var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.gravatar.com/v3/")
        };

        var client = new GravatarClient(httpClient);

        var result = await client.GetProfileAsync(
            "unknown-hash",
            CancellationToken.None);

        Assert.Null(result);
    }

    //
    [Fact]
    public async Task GetProfileAsync_ShouldThrowHttpRequestException_WhenServerReturnsError()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(
                HttpStatusCode.InternalServerError));

        using var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.gravatar.com/v3/")
        };

        var client = new GravatarClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetProfileAsync(
                "test-hash",
                CancellationToken.None));
    }
}
