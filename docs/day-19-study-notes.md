# 19. Gün Çalışma Notları — Docker, README ve Demo Hazırlığı

Bu dosya 19. günde yaptığımız çalışmaları tekrar etmek ve proje sunumuna hazırlanmak içindir.

## 1. Günün amacı

19. günün hedefi, uygulamanın yalnızca kendi bilgisayarımızda değil, Docker kurulu başka bir bilgisayarda da aynı şekilde çalışabilmesidir.

Bu gün üç şey hazırladık:

1. API'yi paketleyen `Dockerfile`.
2. API, SQL Server ve migration işlemini birlikte yöneten `docker-compose.yml`.
3. Kurulum ve sunum için README ve demo dokümanı.

## 2. Docker nedir?

**Docker**, uygulamayı ihtiyaç duyduğu ortamla birlikte çalışan bir pakete koyar. Bu pakete **container** denir.

Bu projede üç container vardır:

| Servis | Görevi |
|---|---|
| `sqlserver` | Scan kayıtlarını tutan SQL Server veritabanı |
| `migrate` | EF Core migration'larını veritabanına uygular |
| `api` | ASP.NET Core API ve web arayüzü |

## 3. Dockerfile

Dosya: `Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DigitalFootprintAuditor.slnx ./
COPY src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj src/DigitalFootprintAuditor.Api/
COPY src/DigitalFootprintAuditor.Application/DigitalFootprintAuditor.Application.csproj src/DigitalFootprintAuditor.Application/
COPY src/DigitalFootprintAuditor.Domain/DigitalFootprintAuditor.Domain.csproj src/DigitalFootprintAuditor.Domain/
COPY src/DigitalFootprintAuditor.Infrastructure/DigitalFootprintAuditor.Infrastructure.csproj src/DigitalFootprintAuditor.Infrastructure/

RUN dotnet restore src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj

COPY src/ src/
RUN dotnet publish src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj --configuration Release --output /app/publish --no-restore
```

Bu ilk bölüm uygulamayı derler ve yayınlanabilir dosyaları `/app/publish` klasörüne koyar.

```dockerfile
FROM build AS migration
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.10
ENTRYPOINT ["/tools/dotnet-ef"]
```

Bu bölüm yalnızca migration çalıştırmak için `dotnet-ef` aracını ekler.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DigitalFootprintAuditor.Api.dll"]
```

Bu son bölüm uygulamayı çalıştırır. `8080` container içindeki uygulama portudur.

### Yeni kavram: çok aşamalı build

Dockerfile'da birden fazla `FROM` kullanmaya **multi-stage build** denir. Derleme araçları son imaja taşınmaz; böylece API imajı daha küçük olur.

## 4. Docker Compose

Dosya: `docker-compose.yml`

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_PID: Express
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
```

- `image`: Kullanılacak hazır SQL Server imajı.
- `environment`: SQL Server'ın başlangıç ayarları.
- `ports`: Bilgisayarın 1433 portunu container'ın 1433 portuna bağlar.
- `volumes`: Veritabanı kayıtlarının container silinse bile kalmasını sağlar.
- `${MSSQL_SA_PASSWORD}`: Parolayı `.env` dosyasından okur.

```yaml
  migrate:
    build:
      context: .
      target: migration
    environment:
      ConnectionStrings__DefaultConnection: Server=sqlserver,1433;Database=DigitalFootprintAuditor;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=False;TrustServerCertificate=True
    depends_on:
      sqlserver:
        condition: service_healthy
    command:
      - database
      - update
      - --project
      - src/DigitalFootprintAuditor.Infrastructure/DigitalFootprintAuditor.Infrastructure.csproj
      - --startup-project
      - src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj
```

Bu servis SQL Server hazır olduktan sonra şu mantıkta bir komut çalıştırır:

```powershell
dotnet ef database update --project src/DigitalFootprintAuditor.Infrastructure/DigitalFootprintAuditor.Infrastructure.csproj --startup-project src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj
```

**Migration**, C# entity sınıflarına göre SQL Server tablolarını oluşturan veya güncelleyen EF Core işlemidir.

```yaml
  api:
    build:
      context: .
      target: final
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: Server=sqlserver,1433;Database=DigitalFootprintAuditor;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=False;TrustServerCertificate=True
    ports:
      - "8080:8080"
    depends_on:
      migrate:
        condition: service_completed_successfully
```

`api`, migration başarıyla bitmeden başlamaz. Bu sıralama önemlidir; aksi halde API veritabanı tabloları oluşmadan çalışmaya başlayabilir.

## 5. Neden SQL Server kullandık?

`Program.cs` içinde şu yapı vardır:

```csharp
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
```

Bu satır uygulamanın EF Core tarafında SQL Server kullandığını gösterir. Bu yüzden eski PostgreSQL örnek bağlantı metnini SQL Server'a çevirdik:

```json
"DefaultConnection": "Server=localhost,1433;Database=DigitalFootprintAuditor;User Id=sa;Password=CHANGE_ME;Encrypt=False;TrustServerCertificate=True"
```

## 6. `.env` ve secret yönetimi

Dosya: `.env.example`

```text
MSSQL_SA_PASSWORD=ChangeThis!12345
```

Kendi bilgisayarında bu dosyadan `.env` oluşturulur:

```powershell
Copy-Item .env.example .env
```

Sonra `.env` içindeki parola değiştirilir:

```text
MSSQL_SA_PASSWORD=Proj3m!Sql2026
```

`.env`, `.gitignore` içinde olduğu için GitHub'a gönderilmez. Bu önemlidir; parola veya API anahtarı kaynak koda yazılmamalıdır.

> Not: `.env.example` örnek dosyadır ve gerçek parola içermez. Bu dosya Git'e eklenebilir.

## 7. Çalıştırma komutları

Proje ana klasöründe PowerShell açılır:

```powershell
cd C:\Users\Lenovo\digital-footprint-auditor
```

İlk kez veya Dockerfile değiştiğinde:

```powershell
docker compose up --build
```

Başarılı çıktıda önemli satırlar:

```text
migrate-1 exited with code 0
api-1 ... Now listening on: http://[::]:8080
```

- `migrate-1 exited with code 0`: Migration başarılıdır.
- `Now listening`: API çalışıyordur.

Tarayıcı adresleri:

```text
http://localhost:8080/
http://localhost:8080/swagger
```

Container'ları durdurup ağı kapatmak için:

```powershell
docker compose down
```

Bu komut veritabanındaki kayıtları silmez. Kayıtları da silmek istersen:

```powershell
docker compose down --volumes
```

> Dikkat: `--volumes` kalıcı SQL Server verisini siler.

## 8. Docker dışındaki normal çalışma

Docker kullanmadan `dotnet run` ile çalışıyorsan adresler farklıdır:

```text
http://localhost:5193/
http://localhost:5193/swagger
```

Docker Compose ile çalışıyorsan adres `8080` olur. Bunun sebebi Compose dosyasındaki `"8080:8080"` port eşlemesidir.

## 9. README ve demo dokümanı

README'ye şunları ekledik:

- Docker Desktop gereksinimi
- `.env` oluşturma ve parola değiştirme adımı
- `docker compose up --build` komutu
- uygulama/Swagger adresleri
- test komutu
- demo akışı bağlantısı

Dosya: `docs/demo-script.md`

Demo sırası:

1. Projenin amacı ve etik sınırlar.
2. Katmanlı mimari ve scanner yapısı.
3. Yeni tarama oluşturma.
4. Risk puanı ve finding'leri açıklama.
5. Tarama geçmişi ve detay sayfası.
6. Unit testler ve hata toleransı.

## 10. Kendini kontrol et

1. Dockerfile ile Docker Compose arasındaki fark nedir?
2. `migrate` servisi neden `api` servisinden önce çalışmalıdır?
3. `.env` dosyası neden GitHub'a gönderilmemelidir?
4. `docker compose down` ile `docker compose down --volumes` arasındaki fark nedir?
5. Docker çalışırken neden 8080, normal `dotnet run` çalışırken 5193 kullanıyoruz?

