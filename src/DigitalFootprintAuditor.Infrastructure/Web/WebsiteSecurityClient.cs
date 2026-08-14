using System.Net;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;

namespace DigitalFootprintAuditor.Infrastructure.Web;

public sealed class WebsiteSecurityClient : IWebsiteSecurityClient
{
    private readonly HttpClient _httpClient;

    public WebsiteSecurityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WebsiteSecurityResult> GetSecurityInfoAsync(
        string domain,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);

        var normalizedInput = domain.Trim();

        if (!Uri.TryCreate(
                normalizedInput,
                UriKind.Absolute,
                out var inputUri))
        {
            normalizedInput = $"https://{normalizedInput}";
        }

        if (!Uri.TryCreate(
                normalizedInput,
                UriKind.Absolute,
                out inputUri))
        {
            throw new ArgumentException(
                "Geçerli bir web sitesi adresi gönderilmelidir.",
                nameof(domain));
        }

        if (inputUri.Scheme != Uri.UriSchemeHttp &&
            inputUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "Yalnızca HTTP veya HTTPS adresleri desteklenir.",
                nameof(domain));
        }

        var host = inputUri.Host;

        var httpUri = new UriBuilder(
            Uri.UriSchemeHttp,
            host).Uri;

        var httpsUri = new UriBuilder(
            Uri.UriSchemeHttps,
            host).Uri;

        try
        {
            using var httpResponse = await _httpClient.GetAsync(
                httpUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var redirectsToHttps = RedirectsToHttps(
                httpResponse,
                httpUri);

            using var httpsResponse = await _httpClient.GetAsync(
                httpsUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var finalUrl =
                httpsResponse.RequestMessage?.RequestUri
                ?? httpsUri;

            var headers = ReadHeaders(httpsResponse);

            return new WebsiteSecurityResult(
                OriginalUrl: inputUri.ToString(),
                FinalUrl: finalUrl.ToString(),
                StatusCode: httpsResponse.StatusCode,
                UsesHttps:
                    finalUrl.Scheme == Uri.UriSchemeHttps,
                RedirectsToHttps: redirectsToHttps,
                Headers: headers);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Web sitesi güvenlik kontrolü zaman aşımına uğradı.",
                exception);
        }
    }

    private static bool RedirectsToHttps(
        HttpResponseMessage response,
        Uri originalHttpUri)
    {
        if (!IsRedirectStatusCode(response.StatusCode))
        {
            return false;
        }

        var location = response.Headers.Location;

        if (location is null)
        {
            return false;
        }

        var redirectUri = location.IsAbsoluteUri
            ? location
            : new Uri(originalHttpUri, location);

        return redirectUri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsRedirectStatusCode(
        HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.MovedPermanently or
            HttpStatusCode.Found or
            HttpStatusCode.SeeOther or
            HttpStatusCode.TemporaryRedirect or
            HttpStatusCode.PermanentRedirect;
    }

    private static IReadOnlyDictionary<string, string> ReadHeaders(
        HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(
                ", ",
                header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(
                ", ",
                header.Value);
        }

        return headers;
    }
}