using DigitalFootprintAuditor.Infrastructure.Gravatar;
using DigitalFootprintAuditor.Infrastructure.GitHub;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using DigitalFootprintAuditor.Infrastructure.Services;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Application.Validators;
using DigitalFootprintAuditor.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Aşama 5 — GitHub dış servis istemcisi
// [x] GitHubClient typed HttpClient olarak kaydedildi.
// [x] GitHub API temel adresi yapılandırıldı.
// [x] GitHub için gerekli User-Agent ve Accept header'ları eklendi.
// [x] Harici servisin sonsuza kadar beklenmemesi için timeout tanımlandı.
// [x] Gün 8: 404, 403/rate limit ve diğer hata durumları yönetilecek.
// [ ] İlerleyen aşamalarda diğer dış servis istemcileri eklenecek.

//github için
builder.Services.AddHttpClient<GitHubClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DigitalFootprintAuditor/1.0");

    client.DefaultRequestHeaders.Accept.ParseAdd(
        "application/vnd.github+json");

    client.Timeout = TimeSpan.FromSeconds(10);
});

//gravatar için
var gravatarBaseUrl = 
    builder.Configuration["ExternalServices:Gravatar:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Gravatar BaseUrl configuration is missing");

builder.Services.AddHttpClient<IGravatarClient, GravatarClient>(client=>
{
    client.BaseAddress = new Uri(gravatarBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

// OpenAPI (Swagger) belge üretimi.
// Development ortamında /swagger adresinden görüntülenir.
builder.Services.AddOpenApi();

// Aşama 3 — EF Core ve veritabanı
// [x] ApplicationDbContext DI sistemine kaydedildi.
// [x] Veritabanı sağlayıcısı olarak SQL Server seçildi.
// [x] DefaultConnection değeri configuration üzerinden okunuyor.
// [ ] İleride yeni entity veya configuration eklenirse DbContext güncellenecek.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Aşama 4 — Application servisleri
// [x] IScanService istendiğinde ScanService kullanılacak.
// [ ] İleride eklenecek application servisleri burada kaydedilecek.
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<IValidator<CreateScanRequestDto>, CreateScanRequestValidator>();
builder.Services.AddScoped<IValidator<ScanTargetInputDto>, ScanTargetInputValidator>();

// Aşama 5 — Scanner kayıtları
// [x] GitHubProfileScanner, IScanner sözleşmesi üzerinden kaydedildi.
// [ ] İlerleyen aşamalarda Gravatar, RDAP, DNS, HTTPS ve diğer scanner'lar eklenecek.
// [ ] Orchestration aşamasında bütün scanner'lar IEnumerable<IScanner>
//     üzerinden toplu şekilde çözümlenecek.
builder.Services.AddScoped<IScanner, GitHubProfileScanner>();
builder.Services.AddScoped<IScanner, GravatarScanner>();

// Controller servislerini ve JSON ayarlarını sisteme tanıtır.
// Enum değerlerinin API response içinde sayı yerine metin olarak gösterilmesini sağlar.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
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

// Uygulamanın ayakta olduğunu doğrulayan basit kök endpoint.
app.MapGet("/", () => Results.Ok(new
{
    name = "Digital Footprint Auditor",
    status = "Running"
}));

// Aşama 4 — Scan API
// [x] Endpointler Controllers/ScansController.cs dosyasına taşındı.
// [x] MapControllers çağrısı controller endpointlerini routing sistemine ekliyor.
// [ ] Yeni controller eklendiğinde ayrıca MapControllers çağrısı eklemek gerekmez.
app.MapControllers();

app.Run();
