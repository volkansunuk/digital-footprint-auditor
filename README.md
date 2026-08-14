# Digital Footprint Auditor

Kendi dijital ayak izinizi ve size ait domainlerin temel güvenlik durumunu analiz eden, **eğitim amaçlı** bir web uygulaması.

Bu repository, iki stajyerin 20 iş günü boyunca geliştirdiği eğitim amaçlı bir uygulamadır.

> **Güncel durum:** Uygulamada scan API'si, GitHub, Gravatar, RDAP, DNS e-posta güvenliği ve HTTPS/header scanner'ları, risk puanlama, temel web arayüzü ve unit testler bulunmaktadır. Aşağıdaki Docker kurulumu mevcut uygulamayı SQL Server ile çalıştırır.

> **Önce şunu okuyun:** [Etik ve Güvenlik Sınırları](#2-etik-ve-güvenlik-sınırları). Bu proje bir kişi araştırma aracı değildir ve asla o yönde geliştirilmeyecektir.

---

## İçindekiler

1. [Projenin Amacı](#1-projenin-amacı)
2. [Etik ve Güvenlik Sınırları](#2-etik-ve-güvenlik-sınırları)
3. [Kurulum ve Çalıştırma](#3-kurulum-ve-çalıştırma)
4. [Teknoloji Seçimi](#4-teknoloji-seçimi)
5. [Katmanlı Mimari](#5-katmanlı-mimari)
6. [Veri Modeli](#6-veri-modeli)
7. [Scanner Yapısı](#7-scanner-yapısı)
8. [Geliştirme Yol Haritası (Aşamalar)](#8-geliştirme-yol-haritası)
9. [20 Günlük Çalışma Planı](#9-20-günlük-çalışma-planı)
10. [İki Kişilik Görev Dağılımı](#10-iki-kişilik-görev-dağılımı)
11. [Git Çalışma Biçimi](#11-git-çalışma-biçimi)
12. [API Taslağı](#12-api-taslağı)
13. [Configuration ve Secret Yönetimi](#13-configuration-ve-secret-yönetimi)
14. [Harici Servis Kullanırken Dikkat Edilecekler](#14-harici-servis-kullanırken-dikkat-edilecekler)
15. [Test Beklentileri](#15-test-beklentileri)
16. [Definition of Done](#16-definition-of-done)
17. [Minimum Teslim Kapsamı](#17-minimum-teslim-kapsamı)
18. [Bonus Görevler](#18-bonus-görevler)
19. [Sık Karşılaşılabilecek Sorunlar](#19-sık-karşılaşılabilecek-sorunlar)
20. [Düşünme Soruları](#20-düşünme-soruları)

---

## 1. Projenin Amacı

Digital Footprint Auditor, kullanıcının **kendisine ait** dijital varlıklar üzerinde temel kontroller yapmasını sağlar.

Kullanıcı sisteme şunları girebilir:

- E-posta adresi
- GitHub kullanıcı adı
- Domain adresi
- Kişisel web sitesi adresi

Sistem, çeşitli **açık ve izinli** kaynaklardan sonuç toplar, bunları ortak bir yapıya dönüştürür ve anlaşılır bir rapor üretir.

Örnek kontroller:

- GitHub açık profil ve public repository analizi
- Gravatar profili kontrolü
- Domain RDAP bilgileri (kayıt tarihi, registrar, nameserver)
- DNS kayıtları: A, AAAA, MX, TXT
- SPF ve DMARC kaydı kontrolü
- HTTPS kullanımı ve temel HTTP güvenlik başlıkları
- (İzinli bir servis varsa) veri ihlali durumu kontrolü
- Tüm bulgulardan **açıklanabilir** bir risk puanı üretme

Bu proje bir kişi araştırma, takip veya profil çıkarma aracı **değildir**.

---

## 2. Etik ve Güvenlik Sınırları

Bu uygulama **eğitim amaçlı, savunmacı ve kullanıcı onayına dayalı** bir araçtır.

Uygulama **yalnızca** şunlar üzerinde kullanılmalıdır:

- Kullanıcının kendisine ait olduğunu beyan ettiği e-posta adresleri
- Kullanıcının kendi GitHub hesapları
- Kullanıcının sahip olduğu veya yönetmeye yetkili olduğu domainler
- Herkese açık ve izinli API kaynakları

Aşağıdaki özellikler proje kapsamında **bulunmayacaktır** ve eklenmeyecektir:

- Telefon numarasından kişi araştırma
- Ad ve soyad üzerinden kişi bulma
- Sosyal medya hesaplarını toplu şekilde arama
- Gizli profillere erişme
- Giriş gerektiren sayfalardan veri çekme
- Yüz tanıma
- Sızdırılmış şifreleri gösterme
- Veri ihlallerindeki hassas verileri görüntüleme
- Toplu e-posta veya kişi taraması
- Başka kişilere ait dijital profilleri izinsiz analiz etme
- Platformların kullanım koşullarını ihlal eden scraping işlemleri

Detaylar için: [docs/ethics-and-safety.md](docs/ethics-and-safety.md)

---

## 3. Kurulum ve Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (güncel LTS sürüm)
- Git
- (Aşama 3'ten itibaren) PostgreSQL veya SQL Server — Docker ile de çalıştırılabilir
- Bir IDE: Visual Studio, Rider veya VS Code (C# Dev Kit eklentisiyle)

### İlk kurulum

```bash
# 1. Repository'yi klonlayın
git clone https://github.com/volkansunuk/digital-footprint-auditor.git
cd digital-footprint-auditor

# 2. SDK sürümünüzü kontrol edin (10.x görmelisiniz)
dotnet --version

# 3. Tüm çözümü derleyin
dotnet build

# 4. API'yi çalıştırın
dotnet run --project src/DigitalFootprintAuditor.Api
```

### Docker ile çalıştırma

Bu proje, mevcut Entity Framework Core yapılandırması nedeniyle **SQL Server** kullanır. Docker Desktop kurulu olmalıdır.

Windows PowerShell:

```powershell
# 1. Yerel SQL Server parolanızı oluşturun
Copy-Item .env.example .env

# 2. .env içindeki MSSQL_SA_PASSWORD değerini güçlü bir parola ile değiştirin

# 3. API, SQL Server ve migration servisini başlatın
docker compose up --build
```

Uygulama `http://localhost:8080/`, Swagger ise `http://localhost:8080/swagger` adresinde açılır.

Kapatmak için:

```bash
docker compose down
```

Veritabanı verilerini de silip tamamen temiz bir başlangıç yapmak için:

```bash
docker compose down --volumes
```

> `docker compose down --volumes` kalıcı SQL Server verisini siler. Normal kullanımda yalnızca `docker compose down` kullanın.

### Testleri çalıştırma

```bash
dotnet test tests/DigitalFootprintAuditor.UnitTests
```

Beklenen sonuç: 25 test başarılı, 0 başarısız.

### Beklenen çıktı

Uygulama başladığında konsolda `Now listening on: http://localhost:XXXX` benzeri bir satır görürsünüz.

- Tarayıcıda `http://localhost:XXXX/` adresine gidin. Yeni tarama ekranı açılmalıdır.

- `http://localhost:XXXX/swagger` adresinde Swagger arayüzü açılmalıdır.

Sunum için adım adım akış: [docs/demo-script.md](docs/demo-script.md)

> **Swagger nedir?** API'nizin tüm endpointlerini otomatik listeleyen ve tarayıcıdan denemenize izin veren bir dokümantasyon arayüzüdür. Postman kullanmadan API'nizi test edebilirsiniz.

### Çözüm dosyası hakkında not

Bu proje yeni `.slnx` çözüm formatını kullanır (`DigitalFootprintAuditor.slnx`). `dotnet build`, Visual Studio ve Rider bu formatı doğrudan destekler; klasik `.sln` ile aynı işi görür.

---

## 4. Teknoloji Seçimi

Ana teknoloji: **.NET 10 (LTS) + ASP.NET Core Web API**

| Teknoloji | Ne için | Ne zaman |
|---|---|---|
| ASP.NET Core Web API | HTTP endpointleri | Aşama 1'den itibaren |
| Entity Framework Core | Veritabanı erişimi (ORM) | Aşama 3 |
| PostgreSQL veya SQL Server | Veritabanı | Aşama 3 |
| Swagger / OpenAPI | API dokümantasyonu | Hazır geliyor |
| `IHttpClientFactory` | Dış API çağrıları | Aşama 5 |
| DnsClient.NET | DNS sorguları | Aşama 8 |
| AngleSharp | HTML parse (gerekirse) | Aşama 9 |
| `IMemoryCache` | Basit cache | Bonus |
| FluentValidation | İstek doğrulama (isteğe bağlı) | Aşama 5. gün |
| xUnit + Moq veya NSubstitute | Unit test | Aşama 5'ten itibaren |
| SignalR | Canlı ilerleme takibi | Bonus |
| Docker | Paketleme | Son aşama |

Kavram açıklamaları (ilk kez duyuyorsanız normal):

- **ORM (EF Core):** SQL cümlelerini elle yazmak yerine C# nesneleri üzerinden veritabanıyla konuşmanızı sağlayan kütüphane.
- **`IHttpClientFactory`:** `HttpClient` nesnelerini doğru ve verimli şekilde yöneten fabrika. `new HttpClient()` yazmak yerine bunu kullanırız (nedenini [Düşünme Soruları](#20-düşünme-soruları) 5. soruda tartışacaksınız).
- **Mock:** Testte gerçek dış servis yerine geçen sahte nesne. Böylece test, internete çıkmadan çalışır.

### Frontend seçimi

İki seçenekten birini seçin — karar sizin, ancak proje **backend ağırlıklı** kalmalıdır:

- **Seçenek A — Razor Pages:** Daha kolay başlangıç. Sayfa tabanlı, öğrenme eğrisi düşük. Önerilen başlangıç.
- **Seçenek B — Blazor Web App:** Biraz daha zorlayıcı, ama frontend'i de C# ile yazarsınız.

---

## 5. Katmanlı Mimari

```text
DigitalFootprintAuditor.slnx

src/
  DigitalFootprintAuditor.Api/             → HTTP endpointleri, başlangıç ayarları
  DigitalFootprintAuditor.Application/     → Akışlar, servis arayüzleri, DTO'lar
  DigitalFootprintAuditor.Domain/          → İş dünyasını temsil eden temel modeller
  DigitalFootprintAuditor.Infrastructure/  → Dış dünya: API istemcileri, veritabanı

tests/
  DigitalFootprintAuditor.UnitTests/       → Unit testler
```

**Katmanlı mimari nedir?** Kodu sorumluluklarına göre ayrı projelere bölmektir. Amaç, "GitHub API'si değişti" gibi bir dış değişikliğin iş kurallarınızı bozmamasıdır.

### Katman sorumlulukları

**Domain** — iş dünyasının dili. `Scan`, `ScanTarget`, `ScanFinding` gibi modeller ve `ScanStatus`, `FindingSeverity` gibi enum'lar burada yaşar. Domain katmanı veritabanını, HTTP istemcisini, GitHub API'sini ve UI'ı **bilmez**. Hiçbir dış pakete bağımlı olmamalıdır.

**Application** — uygulama akışları. `IScanService`, `IFootprintScanner`, `IRiskCalculator` gibi arayüzler ve `ScanRequestDto`, `ScanResultDto` gibi DTO'lar burada.

> **DTO nedir?** DTO (Data Transfer Object), API ile istemci arasında taşınacak veriyi temsil eden sade sınıftır. Veritabanı entity sınıflarını doğrudan dışarı açmamak için kullanılır.

> **Interface (arayüz) neden?** `IScanService` gibi bir arayüz, "bu iş şöyle yapılır" sözleşmesini tanımlar ama nasıl yapıldığını söylemez. Böylece gerçek implementasyonu değiştirebilir veya testte sahtesiyle değiştirebilirsiniz.

**Infrastructure** — dış dünya bağlantıları. GitHub/Gravatar/RDAP istemcileri, DNS sorgu servisi, güvenlik başlığı kontrolü, EF Core `DbContext` ve cache burada.

**Api** — HTTP yüzeyi. Endpointler, Dependency Injection kayıtları, Swagger, global hata yönetimi, configuration ve CORS.

> **Dependency Injection (DI) nedir?** Bir sınıfın ihtiyaç duyduğu servisleri kendisinin oluşturması yerine dışarıdan almasıdır. `Program.cs` içinde "IScanService istenirse ScanService ver" diye kayıt yaparsınız; sınıflar constructor'dan hazır servisi alır.

### "Bu kod hangi katmana?" tablosu

| Yazacağınız kod | Katman |
|---|---|
| `Scan` entity sınıfı | Domain |
| `FindingSeverity` enum'ı | Domain |
| `IFootprintScanner` arayüzü | Application |
| `ScanRequestDto` | Application |
| Risk puanı hesaplama kuralı | Application (veya Domain) |
| GitHub API'sine HTTP isteği atan sınıf | Infrastructure |
| `DbContext` ve entity konfigürasyonları | Infrastructure |
| DNS sorgusu yapan servis | Infrastructure |
| `POST /api/scans` endpoint'i | Api |
| DI kayıtları, Swagger ayarı | Api |
| GitHub'ın JSON cevabını karşılayan model | Infrastructure (Domain'e sızdırmayın!) |

**Bağımlılık yönü:** Api → Application + Infrastructure; Infrastructure → Application; Application → Domain. Domain hiç kimseye bağımlı değildir.

---

## 6. Veri Modeli

Aşağıdaki model bir **öneridir** — kesin ve değiştirilemez tasarım değildir. Gerekçenizi açıklayarak geliştirebilirsiniz.

### Scan — bir tarama işlemi

```text
Id            (Guid)
CreatedAt     (tarama başlangıcı)
CompletedAt   (bitiş, nullable)
Status        (ScanStatus)
RiskScore     (int)
RiskLevel     (RiskLevel)
```

### ScanTarget — neyin tarandığı

```text
Id
ScanId
TargetType    (Email | GitHubUsername | Domain | Website)
TargetValue   (örn. "example.com")
```

### ScanFinding — scanner'ların ürettiği her bulgu

```text
Id
ScanId
ScannerName   (hangi scanner üretti)
Title         (kısa başlık)
Description   (kullanıcıya anlaşılır açıklama)
Severity      (FindingSeverity)
ScoreImpact   (risk puanına katkısı)
Source        (bulgunun kaynağı)
CreatedAt
```

### Enum'lar

```text
ScanStatus:       Pending | Running | Completed | PartiallyCompleted | Failed
FindingSeverity:  Info | Low | Medium | High | Critical
```

> **Enum nedir?** Sınırlı ve bilinen değerler kümesini temsil eden tip. `"Cmpleted"` gibi yazım hatalarını derleme anında yakalar.

---

## 7. Scanner Yapısı

Projenin ana öğretici kısmı budur. **Her kontrol bağımsız bir scanner sınıfıdır** ve hepsi ortak bir arayüzü uygular:

```csharp
public interface IFootprintScanner
{
    string Name { get; }

    bool CanHandle(ScanTargetType targetType);

    Task<IReadOnlyCollection<ScanFinding>> ScanAsync(
        ScanTarget target,
        CancellationToken cancellationToken);
}
```

Bu arayüz neden var?

- Her dış servis **bağımsız** kalır — GitHub kodu RDAP kodunu etkilemez.
- Yeni scanner eklemek kolaydır: arayüzü uygula, DI'a kaydet, bitti.
- Kod tek bir dev sınıfta toplanmaz.
- Unit test yazmak kolaylaşır — her scanner tek başına test edilir.
- Dependency Injection kullanımını pratikte öğrenirsiniz.

Önerilen scanner sınıfları:

```text
GitHubProfileScanner
GitHubRepositoryScanner
GravatarScanner
RdapDomainScanner
DnsSecurityScanner
HttpsScanner
SecurityHeadersScanner
BreachStatusScanner   (API anahtarı yoksa zorunlu değil; mock/fake ile gösterilebilir)
```

---

## 8. Geliştirme Yol Haritası

**Sıra önemlidir.** İlk günden tüm entegrasyonlara aynı anda başlamayın. Her aşamanın "beklenen çıktısı" vardır; onu görmeden sonrakine geçmeyin.

Her aşamayı GitHub'da bir **milestone**, aşama içindeki işleri **issue** olarak açmanızı öneririz (örn. milestone: "Aşama 5 — GitHub Scanner", issue'lar: "Typed HttpClient oluştur", "Profil cevabını finding'e dönüştür", "404 durumunu yönet", "Unit test yaz").

### Aşama 1 — Ortam ve repository düzeni

1. Repository'yi klonlayın, SDK'yı kontrol edin.
2. Çözümü derleyin, katmanları inceleyin.
3. Git branch çalışma biçimini birlikte belirleyin ([bölüm 11](#11-git-çalışma-biçimi)).
4. API'yi çalıştırın, Swagger'ın açıldığını doğrulayın.

Bu aşamada **harici API entegrasyonu yapılmaz**.

> Beklenen çıktı: Uygulama lokal ortamda çalışıyor. Swagger açılıyor. Tüm projeler build oluyor.

### Aşama 2 — Domain modelleri

`Scan`, `ScanTarget`, `ScanFinding` ve enum'ları Domain katmanında oluşturun. Veritabanına geçmeden önce modelin kendisini anlayın: "bir tarama nelerden oluşur?"

> Beklenen çıktı: Bir taramanın hangi bilgilerden oluştuğu kod üzerinde temsil ediliyor.

### Aşama 3 — Veritabanı

1. EF Core paketlerini ekleyin, `DbContext` oluşturun (Infrastructure).
2. Entity konfigürasyonlarını hazırlayın.
3. Connection string'i ayarlayın ([bölüm 13](#13-configuration-ve-secret-yönetimi) — koda gömmeyin!).
4. İlk migration'ı oluşturun ve veritabanını ayağa kaldırın.
5. Basit bir kayıt ekleyip okuyarak doğrulayın.

> **Migration nedir?** C# entity sınıflarınızdaki değişiklikleri veritabanı şemasına uygulayan sürümlü betiklerdir. `dotnet ef migrations add InitialCreate` ile oluşturulur, `dotnet ef database update` ile uygulanır.

> Beklenen çıktı: Scan ve Finding kayıtları veritabanına yazılabiliyor.

### Aşama 4 — Basit Scan API

Harici servislerden **önce** temel API akışı:

```http
POST /api/scans        → yalnızca tarama kaydı oluşturur (henüz scanner çalıştırmaz)
GET  /api/scans/{id}
GET  /api/scans
```

> Beklenen çıktı: Kullanıcı yeni tarama başlatabiliyor ve tarama kaydını görüntüleyebiliyor.

### Aşama 5 — İlk scanner: GitHub Profile

İlk entegrasyon GitHub'dır çünkü dokümantasyonu anlaşılırdır, public veri alınabilir ve JSON cevabı gözlemlemek kolaydır.

1. [GitHub REST API dokümantasyonunu](https://docs.github.com/en/rest/users) inceleyin.
2. Typed `HttpClient` oluşturun (`IHttpClientFactory` üzerinden).
3. Kullanıcı adı ile profil çekin.
4. **GitHub'ın JSON modeli ile Domain modelinizi ayırın** — GitHub cevabını karşılayan sınıf Infrastructure'da kalır.
5. Sonucu `ScanFinding` nesnelerine çevirin.
6. Hata durumlarını yönetin (kullanıcı yok → 404, rate limit → 403).
7. Unit test yazın.

Kontrol edilecek örnekler: profil var mı, public e-posta görünür mü, public repo sayısı, hesabın yaşı, bio, son güncelleme.

> **Önemli:** GitHub aktivitesi (repo sayısı vb.) risk **değildir**. Yalnızca açık e-posta gibi mahremiyet bulguları risk puanına yansıyabilir.

### Aşama 6 — Gravatar scanner

E-postadan hash üretilir, Gravatar profili sorgulanır. Öğrenilecekler: hash üretimi, URL oluşturma, "bulunamadı" cevabını yönetme ve **e-postayı loglarda maskeleme**:

```text
vol***@example.com
```

E-posta adresi loglara asla açık yazılmaz.

### Aşama 7 — Domain RDAP scanner

Kullanıcının girdiği domain için RDAP (WHOIS'in modern hali) sorgusu. Öğrenilecekler: domain normalize etme, URL'den host çıkarma, JSON parse, dış cevabı kendi modele dönüştürme, eksik alanları güvenle yönetme.

Örnek bulgular: kayıt tarihi, son güncelleme, registrar, nameserver'lar, domainin bulunamaması.

### Aşama 8 — DNS Security Scanner

`A`, `AAAA`, `MX`, `TXT` kayıtları kontrol edilir. Özellikle: SPF var mı, DMARC var mı (`_dmarc.example.com` TXT sorgusu), MX var mı.

**Her eksik kayıt kritik güvenlik açığı değildir.** Bulgular açıklayıcı olmalıdır:

- Kötü: `DMARC yok.`
- İyi: `Domain için DMARC kaydı bulunamadı. DMARC, e-posta sahteciliğine karşı koruma sağlamaya yardımcı olur.`

### Aşama 9 — HTTPS ve güvenlik başlıkları

Web sitesine HTTP isteği atılır; şu başlıklar kontrol edilir:

```text
Strict-Transport-Security
Content-Security-Policy
X-Content-Type-Options
Referrer-Policy
Permissions-Policy
```

Ayrıca: HTTPS kullanılıyor mu, HTTP → HTTPS yönlendirmesi var mı, sertifika hatası var mı, zaman aşımı oluyor mu.

> **Sınır:** Bu scanner agresif işlem yapmaz. Port taraması, zafiyet taraması, exploit veya saldırı simülasyonu **yasaktır**.

### Aşama 10 — Scanner orkestrasyonu

Tarama başlatıldığında hedef türüne göre uygun scanner'lar seçilir:

```text
Email girilmişse:            GravatarScanner, BreachStatusScanner
GitHub kullanıcı adı:        GitHubProfileScanner, GitHubRepositoryScanner
Domain girilmişse:           RdapDomainScanner, DnsSecurityScanner, HttpsScanner, SecurityHeadersScanner
```

**Hata toleransı:** Scanner'lar birbirinden bağımsız çalışır. Biri hata verirse tüm tarama çökmemelidir. GitHub çalışıp RDAP hata verirse durum `PartiallyCompleted` olur; kullanıcı hangi scanner'ın başarısız olduğunu görür. Bu, gerçek dünyadaki dağıtık sistemlerin temel prensibidir: dış servisler *her zaman* bir gün hata verir.

### Aşama 11 — Risk puanlama

Risk puanı, bulguların `ScoreImpact` toplamından üretilir. Örnek tablo:

```text
Public e-posta görünür                     +10
SPF bulunamadı                             +10
DMARC bulunamadı                           +15
HTTPS kullanmıyor                          +30
HSTS bulunamadı                            +5
CSP bulunamadı                             +5
Bilinen veri ihlalinde e-posta bulundu     +25
```

Örnek seviyeler:

```text
0–20    Low
21–50   Medium
51–100  High
```

> **Uyarı:** Bu, resmi veya bilimsel bir güvenlik standardı **değildir**; eğitim amaçlı basit bir modeldir.

Risk puanı: açıklanabilir olmalı, her puanın kaynağı gösterilmeli, gizli formül olmamalı ve test edilebilmelidir.

### Aşama 12 — Arayüz

Temel fonksiyonlar çalıştıktan **sonra** arayüz yapılır. Sayfalar:

```text
Ana Sayfa | Yeni Tarama | Tarama Geçmişi | Tarama Detayı | Hakkında ve Etik Kullanım
```

Yeni tarama ekranında e-posta / GitHub kullanıcı adı / domain / website alanları (hiçbiri zorunlu değil). Tarama detayında: genel puan, seviye, tamamlanan ve başarısız scanner'lar, bulgular, açıklamalar, önerilen aksiyonlar.

### Aşama 13 — İlerleme takibi (bonus)

Kullanıcı tarama sırasında durumu görebilir:

```text
GitHub Profile        Completed
Gravatar              Completed
RDAP                  Running
DNS Security          Pending
```

SignalR kullanılabilir; kullanılmazsa frontend belirli aralıklarla scan durumunu sorgulayabilir (polling).

### Aşama 14 — Testler

Bkz. [Test Beklentileri](#15-test-beklentileri).

### Aşama 15 — Docker ve son hazırlık

API için Dockerfile, veritabanı için Docker Compose, environment variable örnekleri, kurulum komutları, demo verileri ve sunum senaryosu.

---

## 9. 20 Günlük Çalışma Planı

| Gün | Odak |
|---|---|
| 1 | Projeyi tanıma, repository kurulumu, Git branch yapısı, API'yi çalıştırma, Swagger kontrolü |
| 2 | Domain kavramları, entity ve enum tasarımı, kısa tasarım sunumu |
| 3 | EF Core kurulumu, DbContext, ilk migration, veritabanı bağlantısı |
| 4 | Scan oluşturma / listeleme / detay endpointleri |
| 5 | DTO ve validation, hata yönetimi, **ilk haftanın code review'u** |
| 6 | `HttpClientFactory`, GitHub API araştırması, GitHub client tasarımı |
| 7 | GitHub scanner geliştirme, API response mapping |
| 8 | GitHub scanner hata yönetimi, unit test |
| 9 | Gravatar scanner, e-posta maskeleme, hash kullanımı |
| 10 | RDAP araştırması, domain normalize etme, RDAP scanner başlangıcı |
| 11 | RDAP scanner tamamlama, hata ve timeout yönetimi |
| 12 | DNS kayıtları, SPF ve DMARC kontrolü |
| 13 | HTTPS kontrolü, Security Headers scanner |
| 14 | Scanner orchestration, kısmi başarısızlık yönetimi |
| 15 | Risk puanlama, finding açıklamaları, **ikinci code review** |
| 16 | Arayüz başlangıcı, yeni tarama ekranı |
| 17 | Tarama geçmişi ve tarama detay ekranı |
| 18 | Unit testlerin tamamlanması, hata senaryoları, temel güvenlik kontrolleri |
| 19 | Docker, README güncellemesi, demo hazırlığı |
| 20 | Proje sunumu, canlı demo, teknik değerlendirme, öğrenilenlerin paylaşımı |

---

## 10. İki Kişilik Görev Dağılımı

### Öğrenci 1

- Domain modelleri
- Entity Framework Core + migration
- Scan API
- GitHub scanner
- Gravatar scanner
- Unit testlerin bir bölümü

### Öğrenci 2

- RDAP scanner
- DNS scanner
- HTTPS scanner
- Security Headers scanner
- Frontend
- Unit testlerin bir bölümü

### Ortak

- Scanner orchestration
- Risk hesaplama
- Code review
- Docker
- README güncellemeleri
- Final demo

> Görevler tamamen ayrışmamalıdır. Her öğrenci diğerinin koduna **en az bir code review** yapmalıdır. Ortak alanlarda (orchestration, risk) birlikte çalışmak, birbirinizin kodunu anlamanızı sağlar.

---

## 11. Git Çalışma Biçimi

Ana branch: `main`

Geliştirme branch örnekleri:

```text
feature/domain-models
feature/github-scanner
feature/dns-scanner
feature/scan-api
feature/frontend
fix/github-timeout
```

Kurallar:

1. Doğrudan `main`'e commit atılmaz.
2. Her görev için ayrı branch açılır.
3. Commit mesajları anlaşılır olur.
4. Pull request açılır.
5. Diğer öğrenci kodu inceler.
6. Uygun görülürse merge edilir.
7. Büyük görevler küçük commitlere bölünür.

İyi commit örnekleri:

```text
feat: add scan domain models
feat: implement GitHub profile client
fix: handle RDAP timeout response
test: add risk calculator tests
docs: update local setup instructions
```

Kötü commit örnekleri:

```text
update
fix
çalıştı
son hali
deneme
```

Tipik akış:

```bash
git checkout main && git pull
git checkout -b feature/github-scanner
# ... geliştirme + küçük commitler ...
git push -u origin feature/github-scanner
# GitHub'da pull request aç, review iste
```

---

## 12. API Taslağı

Başlangıç için önerilen endpointler:

```http
POST   /api/scans
GET    /api/scans
GET    /api/scans/{id}
DELETE /api/scans/{id}
GET    /api/scans/{id}/findings
```

Örnek tarama isteği:

```json
{
  "email": "user@example.com",
  "githubUsername": "sample-user",
  "domain": "example.com",
  "websiteUrl": "https://example.com"
}
```

Örnek ilk cevap:

```json
{
  "id": "2e24c639-4bbf-4e0d-9dc3-b7b1d2789e5c",
  "status": "Running",
  "createdAt": "2026-07-20T10:00:00Z"
}
```

Örnek sonuç:

```json
{
  "id": "2e24c639-4bbf-4e0d-9dc3-b7b1d2789e5c",
  "status": "Completed",
  "riskScore": 35,
  "riskLevel": "Medium",
  "findings": [
    {
      "scannerName": "DnsSecurityScanner",
      "title": "DMARC record not found",
      "description": "The domain does not currently publish a DMARC record.",
      "severity": "Medium",
      "scoreImpact": 15
    }
  ]
}
```

Bu JSON yapıları örnektir; geliştirme sırasında gerekçeli olarak değiştirebilirsiniz. Daha fazla örnek: [docs/api-examples.md](docs/api-examples.md)

---

## 13. Configuration ve Secret Yönetimi

**API anahtarları ve connection string'ler asla kaynak koda yazılmaz.**

Kötü örnek:

```csharp
var apiKey = "my-secret-key"; // ASLA böyle yapmayın
```

Doğru yaklaşımlar:

- **Environment variable** — işletim sistemi seviyesinde tanımlanan değişken; kod `IConfiguration` üzerinden okur.
- **User Secrets** — geliştirme ortamında secret'ları proje dışında tutan .NET mekanizması: `dotnet user-secrets set "ExternalServices:BreachService:ApiKey" "..."`
- **appsettings.Development.json** — lokal ayarlar; `.gitignore`'a eklenirse commit edilmez.

Bu repoda [appsettings.Development.example.json](src/DigitalFootprintAuditor.Api/appsettings.Development.example.json) örneği vardır. Kopyalayıp `appsettings.Development.json` olarak kaydedin ve kendi değerlerinizi girin. **Gerçek secret içeren dosyayı commit etmeyin.**

---

## 14. Harici Servis Kullanırken Dikkat Edilecekler

- **Timeout:** Bir harici servis sonsuza kadar beklenmez. `HttpClient`'a makul bir timeout verin (örn. 10 sn).
- **Rate limit:** API'ler sınırsız kullanılmaz. GitHub anonim isteklerde saatte 60 istekle sınırlıdır; limit dolunca 403 alırsınız.
- **Retry:** Her hata tekrar denenmez. Geçici hatalar (ağ kopması) denenebilir; kalıcı hatalar (hatalı kullanıcı adı → 404) **asla** retry edilmez.
- **CancellationToken:** İstek iptal edildiğinde dış API çağrıları da iptal edilebilmelidir. `ScanAsync` imzasındaki token'ı dış çağrılara iletin.
- **User-Agent:** GitHub gibi servisler `User-Agent` başlığı olmayan istekleri reddedebilir.
- **Cache:** Aynı domain/kullanıcı adı kısa sürede tekrar sorgulanıyorsa sonuç geçici olarak cache'lenebilir (`IMemoryCache`).
- **Logging:** Loglara şunlar **yazılmaz**: tam e-posta adresi, API anahtarı, connection string, hassas kullanıcı girdileri, harici API'nin döndürdüğü gereksiz kişisel veriler.

---

## 15. Test Beklentileri

En az şu testler beklenir:

- Risk puanı hesaplama testi
- GitHub cevabının finding nesnesine dönüşüm testi
- Domain normalize etme testi
- Geçersiz URL testi
- Bir scanner hata verdiğinde diğerlerinin devam etmesi testi
- Hedef türüne göre doğru scanner seçimi testi

Kurallar:

- Harici API'lere **gerçek istek atan** testler unit test değildir. Mock veya fake response kullanın.
- Test projesi hazır: `tests/DigitalFootprintAuditor.UnitTests` (xUnit). Mock için Moq veya NSubstitute ekleyin.

```bash
dotnet test
```

---

## 16. Definition of Done

Bir görev "tamamlandı" sayılmadan önce:

- [ ] Kod build oluyor.
- [ ] İlgili endpoint çalışıyor.
- [ ] Hata senaryosu düşünülmüş.
- [ ] Gerekli validation eklenmiş.
- [ ] Secret kaynak koda yazılmamış.
- [ ] Kod diğer öğrenci tarafından incelenmiş.
- [ ] Gerekiyorsa test yazılmış.
- [ ] README veya ilgili doküman güncellenmiş.
- [ ] Pull request açıklaması eklenmiş.

---

## 17. Minimum Teslim Kapsamı

Staj sonunda mutlaka bulunması gerekenler:

- Çalışan ASP.NET Core uygulaması
- Veritabanı
- Scan oluşturma ve görüntüleme
- **En az dört scanner**, bunların içinde:
  - GitHub scanner
  - RDAP veya DNS scanner
  - HTTPS veya Security Headers scanner
- Risk puanlama
- Basit frontend
- Hata yönetimi
- Swagger
- En az beş anlamlı unit test
- Git branch ve pull request kullanımı
- Kurulum dokümantasyonu
- Demo

---

## 18. Bonus Görevler

**Ana görevler tamamlanmadan bonuslara başlanmaz.**

- SignalR ile canlı tarama ilerlemesi
- PDF / HTML raporu üretme ve dışa aktarma
- Tarama sonuçlarını karşılaştırma (önceki–sonraki fark görünümü)
- Finding'leri "çözüldü" olarak işaretleme
- Basit kullanıcı girişi
- Docker Compose
- Health Check
- Memory Cache
- Retry ve timeout politikaları
- Dark mode
- Grafiklerle risk dağılımı

---

## 19. Sık Karşılaşılabilecek Sorunlar

**`dotnet: command not found`** — .NET SDK kurulu değil veya PATH'te değil. SDK'yı kurup terminali yeniden başlatın.

**Swagger açılmıyor** — Uygulamanın Development ortamında çalıştığından emin olun (`ASPNETCORE_ENVIRONMENT=Development`). Doğru portu konsol çıktısından kontrol edin; adres `/swagger`.

**HTTPS sertifika uyarısı** — Lokal geliştirme sertifikasına güvenin: `dotnet dev-certs https --trust`

**GitHub API 403 dönüyor** — Muhtemelen rate limit (anonim: 60 istek/saat) veya eksik `User-Agent` başlığı. Cevaptaki `X-RateLimit-Remaining` başlığına bakın.

**`dotnet ef` bulunamıyor** — EF araç paketini kurun: `dotnet tool install --global dotnet-ef`

**Migration hatası: DbContext bulunamadı** — `dotnet ef` komutlarını `--project` (Infrastructure) ve `--startup-project` (Api) parametreleriyle çalıştırın.

**Veritabanına bağlanamıyor** — Connection string'i ve veritabanının ayakta olduğunu kontrol edin. Docker kullanıyorsanız `docker ps` ile container'ın çalıştığını doğrulayın.

**Port çakışması (`address already in use`)** — Aynı portu kullanan başka bir uygulama açık. `Properties/launchSettings.json` içindeki portu değiştirin.

**Türkçe karakter sorunları** — Dosyaların UTF-8 kaydedildiğinden emin olun.

---

## 20. Düşünme Soruları

Staj sonunda bu soruları cevaplayabiliyor olmalısınız. Sunumda birkaçı sorulacaktır:

1. Neden veritabanı entity'lerini doğrudan API response olarak dönmemeliyiz?
2. Her scanner neden ayrı bir sınıf olmalıdır?
3. Bir scanner hata verdiğinde tüm tarama neden durmamalıdır?
4. Harici API modelleri neden Domain modellerinden ayrı tutulmalıdır?
5. `HttpClient` neden her istekte `new` ile oluşturulmamalıdır?
6. API anahtarları neden GitHub repository içerisine eklenmemelidir?
7. SPF veya DMARC kaydının bulunmaması neden her zaman kritik seviye değildir?
8. Unit testlerde neden gerçek GitHub API'sine istek atmamalıyız?
9. Risk puanının açıklanabilir olması neden önemlidir?
10. Kullanıcının verdiği e-posta adresini loglarda neden maskelemeliyiz?

---

## Ek Dokümanlar

- [docs/architecture.md](docs/architecture.md) — katman ve bağımlılık detayları
- [docs/api-examples.md](docs/api-examples.md) — örnek istek/cevaplar
- [docs/ethics-and-safety.md](docs/ethics-and-safety.md) — etik kullanım sözleşmesi

İyi çalışmalar! 🚀
