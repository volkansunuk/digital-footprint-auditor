var builder = WebApplication.CreateBuilder(args);

// OpenAPI (Swagger) belge üretimi. Geliştirme ortamında /swagger adresinden görüntülenir.
builder.Services.AddOpenApi();

// TODO (Aşama 3+): EF Core DbContext kaydı buraya eklenecek.
// TODO (Aşama 4+): Application servisleri (IScanService vb.) buraya eklenecek.
// TODO (Aşama 5+): Scanner servisleri ve HttpClientFactory kayıtları buraya eklenecek.

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

app.Run();
