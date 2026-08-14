using System.Net;
using DigitalFootprintAuditor.Infrastructure.Web;

namespace DigitalFootprintAuditor.UnitTests.Web;

public class WebsiteSecurityClientTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage,
                CancellationToken,
                Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }


    [Fact]
    public async Task GetSecurityInfoAsync_ShouldDetectHttpsRedirectAndReadHeaders()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            (request, _) =>
            {
                if (request.RequestUri?.Scheme == Uri.UriSchemeHttp)
                {
                    var redirectResponse =
                        new HttpResponseMessage(
                            HttpStatusCode.MovedPermanently);

                    redirectResponse.Headers.Location =
                        new Uri("https://example.com");

                    redirectResponse.RequestMessage = request;

                    return Task.FromResult(redirectResponse);
                }

                var httpsResponse =
                    new HttpResponseMessage(HttpStatusCode.OK);

                httpsResponse.Headers.TryAddWithoutValidation(
                    "Strict-Transport-Security",
                    "max-age=31536000");

                httpsResponse.Headers.TryAddWithoutValidation(
                    "Content-Security-Policy",
                    "default-src 'self'");

                httpsResponse.RequestMessage = request;

                return Task.FromResult(httpsResponse);
            });

        var httpClient = new HttpClient(handler);

        var client = new WebsiteSecurityClient(httpClient);

        // Act
        var result = await client.GetSecurityInfoAsync(
            "example.com",
            CancellationToken.None);

        // Assert
        Assert.True(result.UsesHttps);
        Assert.True(result.RedirectsToHttps);
        Assert.Equal(
            HttpStatusCode.OK,
            result.StatusCode);

        Assert.Contains(
            "Strict-Transport-Security",
            result.Headers.Keys);

        Assert.Contains(
            "Content-Security-Policy",
            result.Headers.Keys);
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldReportNoRedirect_WhenHttpDoesNotRedirect()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            (request, _) =>
            {
                var response =
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request
                    };

                return Task.FromResult(response);
            });

        var client = new WebsiteSecurityClient(
            new HttpClient(handler));

        // Act
        var result = await client.GetSecurityInfoAsync(
            "example.com",
            CancellationToken.None);

        // Assert
        Assert.True(result.UsesHttps);
        Assert.False(result.RedirectsToHttps);
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldHandleRelativeHttpsRedirectLocation()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            (request, _) =>
            {
                if (request.RequestUri?.Scheme == Uri.UriSchemeHttp)
                {
                    var response =
                        new HttpResponseMessage(
                            HttpStatusCode.MovedPermanently)
                        {
                            RequestMessage = request
                        };

                    response.Headers.Location =
                        new Uri(
                            "https://example.com/home",
                            UriKind.Absolute);

                    return Task.FromResult(response);
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request
                    });
            });

        var client = new WebsiteSecurityClient(
            new HttpClient(handler));

        // Act
        var result = await client.GetSecurityInfoAsync(
            "example.com",
            CancellationToken.None);

        // Assert
        Assert.True(result.RedirectsToHttps);
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldThrowArgumentException_WhenSchemeIsUnsupported()
    {
        //arrange
        var client = new WebsiteSecurityClient(new HttpClient(new StubHttpMessageHandler((_, _) =>
            throw new   InvalidOperationException(
                "Http çağrısı yapılmamalı."
            )))); 

        //act & assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetSecurityInfoAsync(
                "ftp://example.com", 
                CancellationToken.None));
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldThrowArgumentException_WhenDomainIsEmpty()
    {
        var client = new WebsiteSecurityClient(
            new HttpClient(
            new StubHttpMessageHandler(
            (_, _) => 
            throw new InvalidOperationException(
                "Http çağrısı yapılmamalı."))));

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetSecurityInfoAsync(
                " ",
                CancellationToken.None));
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldThrowTimeoutException_WhenRequestTimesout()
    {
        // arrange
        var handler = new StubHttpMessageHandler(
            (_, _) => 
            throw new TaskCanceledException(
                "İstek zaman aşımına uğradı."));

        var httpClient = new HttpClient(handler);
        var client = new WebsiteSecurityClient(httpClient);

        //await & assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => client.GetSecurityInfoAsync(
                "example.com",
                CancellationToken.None));
    }

    [Fact]
    public async Task GetSecurityInfoAsync_ShouldPropagateCancellation_WhenCallerCancelsRequest()
    {
        //arrange
        var handler = new StubHttpMessageHandler(
            (_, cancellationToken) => 
            Task.FromCanceled<HttpResponseMessage>(
                cancellationToken));

        var client = new WebsiteSecurityClient(
            new HttpClient(handler));

        using var cancellationTokenSource = 
        new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        //act & assert

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSecurityInfoAsync(
                "example.com",
                cancellationTokenSource.Token));
    }
}

