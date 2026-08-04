using System.Net;

namespace DigitalFootprintAuditor.Application.Models;

public sealed record WebsiteSecurityResult(
    string OriginalUrl,
    string FinalUrl,
    HttpStatusCode StatusCode,
    bool UsesHttps,
    bool RedirectsToHttps,
    IReadOnlyDictionary<string, string> Headers
);