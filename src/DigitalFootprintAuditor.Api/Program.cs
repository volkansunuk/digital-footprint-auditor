using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Infrastructure.Persistence;
using DigitalFootprintAuditor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Swagger) belge üretimi. Geliştirme ortamında /swagger adresinden görüntülenir.
builder.Services.AddOpenApi();

// TODO (Aşama 3+): EF Core DbContext kaydı buraya eklenecek.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    /* 
    AddDbContext<ApplicationDbContext>: Uygulamaya "Sana veritabanı işlemleri için 
    ApplicationDbContext sınıfını emanet ediyorum" dedik.
    UseSqlServer(...): "Veritabanı sürücüsü olarak SQL Server kullan" dedik.
    GetConnectionString("DefaultConnection"): "Bağlantı adresi olarak da az önce appsettings.json içine
     yazdığımız DefaultConnection anahtarındaki adresi oku" dedik.*/

// TODO (Aşama 4+): Application servisleri (IScanService vb.) buraya eklenecek.
builder.Services.AddScoped<IScanService, ScanService>();

// TODO (Aşama 5+): Scanner servisleri ve HttpClientFactory kayıtları buraya eklenecek.
// Her scanner IScanner arayüzü üzerinden kaydediliyor ki ileride orkestrasyon
// servisi IEnumerable<IScanner> ile hepsine tek seferde erişebilsin.
builder.Services.AddHttpClient<IScanner, GitHubProfileScanner>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "DigitalFootprintAuditor");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Controller servislerini ve API Explorer'ı sisteme tanıtır
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Digital Footprint Auditor API v1");
    });
}

app.UseHttpsRedirection();

// Uygulamanın ayakta olduğunu doğrulayan basit kök endpoint.
app.MapGet("/", () => Results.Ok(new
{
    name = "Digital Footprint Auditor",
    status = "Running"
}));

// TODO (Aşama 4): /api/scans endpointleri burada (veya ayrı bir dosyada) tanımlanacak.
// Endpointler ayrı bir dosyada (Controllers/ScansController.cs) tanımlandı;
// aşağıdaki satır o controller'ı API yönlendirme haritasına ekler (KRİTİK ADIM).
app.MapControllers();

app.Run();