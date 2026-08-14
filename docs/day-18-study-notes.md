# 18. Gün Çalışma Notları — Test, Hata Senaryosu ve Temel Güvenlik

Bu dosya 18. günde yaptığımız kod değişikliklerini öğrenmek için hazırlanmıştır.

## 1. Amaç

18. günün üç hedefi vardı:

1. Mevcut kodun önemli davranışlarını **unit test** ile korumak.
2. Hata senaryolarında uygulamanın beklenen davranışını doğrulamak.
3. Arayüzde dışarıdan gelen metinleri güvenli biçimde göstermek.

Testler gerçek API'lere istek atmaz. Bunun yerine `Moq` ile sahte bağımlılıklar kullanılır. Böylece test hızlı, ücretsiz ve tekrar edilebilir olur.

## 2. Risk puanı testleri

Dosya: `tests/DigitalFootprintAuditor.UnitTests/RiskScoringServiceTests.cs`

```csharp
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Services;

namespace DigitalFootprintAuditor.UnitTests.Services;

public class RiskScoringServiceTests
{
    private readonly RiskScoringService _service = new();

    [Theory]
    [InlineData(20, RiskLevel.Low)]
    [InlineData(21, RiskLevel.Medium)]
    [InlineData(50, RiskLevel.Medium)]
    [InlineData(51, RiskLevel.High)]
    public void CalculateRisk_BoundaryScores_ReturnsExpectedRiskLevel(int score, RiskLevel expectedLevel)
    {
        var result = _service.CalculateRisk([score]);

        Assert.Equal(score, result.RiskScore);
        Assert.Equal(expectedLevel, result.RiskLevel);
    }

    [Fact]
    public void CalculateRisk_ScoreExceedsMaximum_CapsScoreAtOneHundred()
    {
        var result = _service.CalculateRisk([75, 50]);

        Assert.Equal(100, result.RiskScore);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
    }
}
```

### Önemli kavramlar

- **`[Theory]`**: Aynı test metodunu farklı verilerle çalıştırır.
- **`[InlineData]`**: Theory'ye gönderilecek test verisidir.
- **Sınır değeri (boundary)**: Bir kuralın değiştiği noktadır. Burada 20/21 ve 50/51 kritik sınırlardır.
- **`Assert.Equal`**: Beklenen değer ile gerçek değerin aynı olduğunu kontrol eder.

Bu testler şunu garanti eder: Risk seviyesi yanlışlıkla 20 yerine 21'de veya 50 yerine 51'de değişirse test hata verir.

## 3. Timeout ve kullanıcı iptali testi

Dosya: `tests/DigitalFootprintAuditor.UnitTests/Utilities/HttpExceptionHelperTests.cs`

```csharp
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.UnitTests.Utilities;

public class HttpExceptionHelperTests
{
    [Fact]
    public void IsTimeout_TaskCanceledWithoutUserCancellation_ReturnsTrue()
    {
        var isTimeout = HttpExceptionHelper.IsTimeout(
            new TaskCanceledException(),
            CancellationToken.None);

        Assert.True(isTimeout);
    }

    [Fact]
    public void IsTimeout_UserCancellationRequested_ReturnsFalse()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var isTimeout = HttpExceptionHelper.IsTimeout(
            new TaskCanceledException(),
            cancellationSource.Token);

        Assert.False(isTimeout);
    }
}
```

### Neden gerekli?

`HttpClient` zaman aşımında bazen `TaskCanceledException` fırlatır. Ancak aynı hata türü kullanıcı gerçekten işlemi iptal ettiğinde de oluşabilir.

- Token iptal edilmemişse: bu durumu **timeout** kabul ederiz.
- Token iptal edilmişse: bu durum **kullanıcı iptali**dir; timeout değildir.

### Yeni kavramlar

- **`CancellationTokenSource`**: Bir iptal sinyali üretir.
- **`Cancel()`**: İptal isteğini başlatır.
- **`using`**: İş bittiğinde `CancellationTokenSource` nesnesinin kaynaklarını otomatik temizler.

## 4. Scanner orchestration testi

Dosya: `tests/DigitalFootprintAuditor.UnitTests/ScanOrchestratorTests.cs`

```csharp
[Fact]
public async Task RunScannersAsync_OneScannerFails_OtherScannerContinuesAndScanIsPartial()
{
    var failingScanner = new StubScanner(
        ScanTargetType.Domain,
        _ => throw new HttpRequestException("Bağlantı hatası"));

    var successfulScanner = new StubScanner(
        ScanTargetType.Domain,
        _ =>
        [
            new ScanFindingResult
            {
                ScannerName = "SuccessfulScanner",
                Title = "Başarılı bulgu",
                Description = "Tarama devam etti.",
                Severity = FindingSeverity.Info,
                Source = "Test"
            }
        ]);

    var logger = new Mock<ILogger<ScanOrchestrator>>();
    var orchestrator = new ScanOrchestrator([failingScanner, successfulScanner], logger.Object);
    var targets = new List<ScanTarget>
    {
        new() { TargetType = ScanTargetType.Domain, TargetValue = "example.com" }
    };

    var result = await orchestrator.RunScannersAsync(targets, CancellationToken.None);

    Assert.True(result.HadUnexpectedFailures);
    Assert.Contains(result.Findings, finding => finding.ScannerName == "SuccessfulScanner");
    Assert.Contains(result.Findings, finding => finding.Source == "System");
}
```

Bu testte kullanılan `StubScanner`, gerçek GitHub veya RDAP isteği atmaz. Test için oluşturulmuş küçük bir scanner taklididir:

```csharp
private sealed class StubScanner : IFootprintScanner
{
    private readonly Func<string, List<ScanFindingResult>> _scan;

    public StubScanner(ScanTargetType supportedTargetType, Func<string, List<ScanFindingResult>> scan)
    {
        SupportedTargetType = supportedTargetType;
        _scan = scan;
    }

    public ScanTargetType SupportedTargetType { get; }

    public Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
        => Task.FromResult(_scan(targetValue));
}
```

### Önemli kavramlar

- **Stub**: Test sırasında gerçek bağımlılığın yerine geçen basit nesne.
- **`Func<string, List<ScanFindingResult>>`**: Bir `string` alıp `List<ScanFindingResult>` döndüren metot tipidir.
- **`async` / `await`**: Asenkron metodu beklerken thread'i bloklamaz.
- **`Task.FromResult(...)`**: Hazır olan sonucu `Task` biçimine dönüştürür.

Bu testin mesajı şudur: RDAP veya DNS gibi bir servis hata verdiğinde bütün scan kaydı başarısız olmamalıdır. Diğer scanner'lar çalışmalı ve sonuç `PartiallyCompleted` olabilir.

## 5. Arayüzde XSS önlemi

Önceki yaklaşımda API'den gelen metin HTML şablonunun içine ekleniyordu:

```javascript
// Güvenli olmayan yaklaşım
container.innerHTML = `<p>${finding.description}</p>`;
```

Eğer `finding.description` içinde HTML veya JavaScript benzeri zararlı bir içerik olursa, tarayıcı bunu HTML olarak yorumlayabilir. Buna **XSS (Cross-Site Scripting)** denir.

Yeni yaklaşım:

```javascript
function appendTextElement(parent, tagName, text, className = '') {
    const element = document.createElement(tagName);
    element.textContent = text;
    element.className = className;
    parent.appendChild(element);
    return element;
}

const findingElement = document.createElement('div');
findingElement.className = 'finding';
appendTextElement(findingElement, 'strong', finding.title);
appendTextElement(findingElement, 'p', finding.description);
container.appendChild(findingElement);
```

### Fark nedir?

- **`innerHTML`**: Metni HTML olarak yorumlar.
- **`textContent`**: Metni sadece yazı olarak gösterir.
- **`document.createElement`**: JavaScript ile güvenli biçimde HTML elemanı oluşturur.

Bu yaklaşım `app.js`, `history.js` ve `detail.js` dosyalarında kullanıldı.

## 6. Çalıştırılan doğrulamalar

```powershell
dotnet test tests/DigitalFootprintAuditor.UnitTests
```

Sonuç: **25 test başarılı, 0 başarısız.**

JavaScript dosyaları için de sözdizimi kontrolü yapıldı:

```powershell
node --check src/DigitalFootprintAuditor.Api/wwwroot/app.js
node --check src/DigitalFootprintAuditor.Api/wwwroot/history.js
node --check src/DigitalFootprintAuditor.Api/wwwroot/detail.js
```

## 7. Kendini kontrol et

1. Neden 20 ve 21 için ayrı test yazmak önemlidir?
2. `TaskCanceledException` neden her zaman timeout anlamına gelmez?
3. Stub ile mock arasındaki temel fark nedir?
4. Bir scanner hata verdiğinde diğer scanner'lar neden devam etmelidir?
5. `textContent`, `innerHTML`'den neden daha güvenlidir?

