using DigitalFootprintAuditor.Infrastructure.ExternalClients.Security;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.Dns;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.Rdap;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.Gravatar;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Infrastructure.Services;
using DigitalFootprintAuditor.Application.Dtos;
using System.ComponentModel.DataAnnotations;
using DigitalFootprintAuditor.Api;
using DigitalFootprintAuditor.Infrastructure.ExternalClients.GitHub;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Swagger) belge üretimi. Geliştirme ortamında /swagger adresinden görüntülenir.
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// TODO (Aşama 3+): EF Core DbContext kaydı buraya eklenecek.
builder.Services.AddDbContext<AuditorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// TODO (Aşama 4+): Application servisleri (IScanService vb.) buraya eklenecek.
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddScoped<IScanOrchestrator, ScanOrchestrator>();
// TODO (Aşama 5+): Scanner servisleri ve HttpClientFactory kayıtları buraya eklenecek.

builder.Services.AddHttpClient<IGitHubApiClient, GitHubApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "DigitalFootprintAuditor");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IFootprintScanner, GitHubExposureScanner>();
builder.Services.AddHttpClient<IGravatarApiClient, GravatarApiClient>(client =>
{
    client.BaseAddress = new Uri("https://www.gravatar.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IFootprintScanner, GravatarScanner>();
builder.Services.AddHttpClient<IRdapApiClient, RdapApiClient>(client =>
{
    client.BaseAddress = new Uri("https://rdap.org/");
    client.DefaultRequestHeaders.Add("User-Agent", "DigitalFootprintAuditor/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/rdap+json");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IFootprintScanner, RdapScanner>();
builder.Services.AddHttpClient<IDnsApiClient, DnsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://dns.google/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IFootprintScanner, EmailSecurityScanner>();
builder.Services.AddHttpClient<ISecurityHeaderClient, SecurityHeaderClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true
    });
builder.Services.AddScoped<IFootprintScanner, HttpsSecurityScanner>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Digital Footprint Auditor API v1");
    });
}

app.UseHttpsRedirection();

// TODO (Aşama 4): /api/scans endpointleri burada (veya ayrı bir dosyada) tanımlanacak.
app.MapPost("/api/scans", async (CreateScanRequest request, IScanService scanService, CancellationToken cancellationToken) =>
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true);

    if (!isValid)
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Targets", errors.ToArray()! }
        });
    }

    var result = await scanService.CreateScanAsync(request, cancellationToken);
    return Results.Created($"/api/scans/{result.Id}", result);
});

app.MapGet("/api/scans/{id:guid}", async (Guid id, IScanService scanService, CancellationToken cancellationToken) =>
{
    var result = await scanService.GetScanByIdAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/scans", async (IScanService scanService, CancellationToken cancellationToken) =>
{
    var results = await scanService.GetAllScansAsync(cancellationToken);
    return Results.Ok(results);
});
app.MapDelete("/api/scans/{id:guid}", async (Guid id, IScanService scanService, CancellationToken cancellationToken) =>
{
    var deleted = await scanService.DeleteScanAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/test-github/{username}", async (string username, IGitHubApiClient gitHubApiClient, CancellationToken cancellationToken) =>
{
    var result = await gitHubApiClient.GetUserInfoAsync(username, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});


app.Run();
