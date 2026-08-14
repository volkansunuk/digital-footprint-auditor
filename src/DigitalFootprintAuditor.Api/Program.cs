using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Application.Validators;
using DigitalFootprintAuditor.Application.Services;
using DigitalFootprintAuditor.Infrastructure.Gravatar;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Infrastructure.Rdap;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using DigitalFootprintAuditor.Infrastructure.Services;
using DigitalFootprintAuditor.Infrastructure.Dns;
using DigitalFootprintAuditor.Api.Hubs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using DnsClient;
using DigitalFootprintAuditor.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

// Aşama 5 — GitHub dış servis istemcisi
builder.Services.AddHttpClient<GitHubClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DigitalFootprintAuditor/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// OpenAPI (Swagger) belge üretimi.
// Geliştirme ortamında /swagger adresinden görüntülenir.
builder.Services.AddOpenApi();

var gravatarBaseUrl =
    builder.Configuration["ExternalServices:Gravatar:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Gravatar BaseUrl configuration is missing");

builder.Services.AddHttpClient<IGravatarClient, GravatarClient>(client =>
{
    client.BaseAddress = new Uri(gravatarBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

//rdap için
builder.Services.AddHttpClient<IRdapClient, RdapClient>(client =>
{
    client.BaseAddress = new Uri("https://rdap.org/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/rdap+json");
});

//website security için
builder.Services
    .AddHttpClient<IWebsiteSecurityClient, WebsiteSecurityClient>(
        client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

// Aşama 3 — EF Core ve veritabanı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Aşama 4 — Application servisleri
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<IRiskCalculator, RiskScoringService>();
builder.Services.AddScoped<IValidator<CreateScanRequestDto>, CreateScanRequestValidator>();
builder.Services.AddScoped<IValidator<ScanTargetInputDto>, ScanTargetInputValidator>();



// Aşama 5 — Scanner kayıtları
builder.Services.AddScoped<IScanner, GitHubProfileScanner>();
builder.Services.AddScoped<IScanner, GravatarScanner>();
builder.Services.AddScoped<IScanner, RdapDomainScanner>();
builder.Services.AddScoped<IScanner, DnsSecurityScanner>();
builder.Services.AddScoped<IScanner, HttpsScanner>();
builder.Services.AddScoped<IScanner, SecurityHeadersScanner>();
builder.Services.AddScoped<IScanner, GitHubRepositoryScanner>();
builder.Services.AddScoped<
    IScanProgressNotifier,
    SignalRScanProgressNotifier>();

//dns istemcisi
builder.Services.AddSingleton<DnsClient.LookupClient>();

builder.Services.AddScoped<
    IDnsClient,
    DigitalFootprintAuditor.Infrastructure.Dns.DnsClient>();


// Controller servislerini ve JSON ayarlarını sisteme tanıtır.
// Enum değerlerinin API response içinde sayı yerine metin olarak gösterilmesini sağlar.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddRazorPages();

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Digital Footprint Auditor API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Uygulamanın ayakta olduğunu doğrulayan basit kök endpoint.
app.MapGet("/api/health", () => Results.Ok(new
{
    name = "Digital Footprint Auditor",
    status = "Running"
}));

// Aşama 4 — Scan API
app.MapControllers();

app.MapRazorPages();

app.MapHub<ScanProgressHub>("/hubs/scan-progress");

app.Run();
