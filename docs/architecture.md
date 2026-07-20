# Mimari

## Katmanlar ve bağımlılık yönü

```text
Api ──────────────► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘
```

- **Domain**: `Scan`, `ScanTarget`, `ScanFinding`, enum'lar. Hiçbir pakete ve katmana bağımlı değildir.
- **Application**: `IScanService`, `IFootprintScanner`, `IRiskCalculator` arayüzleri; DTO'lar; akış mantığı. Yalnızca Domain'e bağımlıdır.
- **Infrastructure**: GitHub/Gravatar/RDAP istemcileri, DNS servisi, EF Core `DbContext`, cache. Application'daki arayüzleri uygular.
- **Api**: Endpointler, DI kayıtları, Swagger, global hata yönetimi, configuration, CORS.

## Neden bu yön?

Bağımlılıklar hep "dışarıdan içeriye" akar. GitHub API'si değişirse yalnızca Infrastructure değişir;
Domain ve Application etkilenmez. Testte Infrastructure'daki gerçek implementasyonlar yerine mock
konabilir, çünkü Application yalnızca arayüz tanır.

## Scanner akışı (hedef tasarım)

```text
POST /api/scans
  → ScanService tarama kaydını oluşturur (Pending)
  → Hedef türlerine göre uygun IFootprintScanner'lar seçilir (CanHandle)
  → Her scanner bağımsız çalışır; bulgular ScanFinding olarak toplanır
  → Bir scanner hata verirse diğerleri devam eder (PartiallyCompleted)
  → IRiskCalculator bulgulardan puan ve seviye üretir
  → Sonuç veritabanına yazılır (Completed / PartiallyCompleted / Failed)
```

Bu belge yaşayan bir dokümandır — tasarım kararlarınızı gerekçeleriyle buraya ekleyin.
