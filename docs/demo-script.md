# Digital Footprint Auditor — Demo Akışı

Bu akış yaklaşık 5–7 dakikalık sunum içindir. Yalnızca size ait veya tarama izniniz olan hedefleri kullanın.

## Demo öncesi kontrol

1. Uygulamayı başlatın: `docker compose up --build`.
2. Tarayıcıda `http://localhost:8080/` adresini açın.
3. Swagger'ın açıldığını doğrulamak için `http://localhost:8080/swagger` adresine gidin.
4. İnternet bağlantısının açık olduğundan emin olun; scanner'lar GitHub, Gravatar, RDAP, DNS ve HTTP kaynaklarına istek atar.

## Sunum sırası

1. **Amaç ve etik sınır**
   - Uygulama, kullanıcının kendi dijital varlıklarını savunmacı amaçla incelemesi içindir.
   - Port taraması, exploit ve izinsiz kişi araştırması yapmaz.

2. **Mimari**
   - API, Application, Domain ve Infrastructure katmanlarını gösterin.
   - Scanner'ların `IFootprintScanner` sözleşmesi üzerinden bağımsız çalıştığını açıklayın.

3. **Yeni tarama**
   - Ana sayfada size ait bir domain ve website girin.
   - İsterseniz size ait GitHub kullanıcı adı veya e-posta hedefi ekleyin.
   - “Taramayı Başlat” düğmesine basın.

4. **Sonuçlar**
   - Risk puanını ve risk seviyesini gösterin.
   - RDAP, SPF/DMARC veya HTTPS header bulgularından en az birini açıklayın.
   - Her bulgunun `ScoreImpact` değeriyle açıklanabilir risk puanına katkı yaptığını belirtin.

5. **Geçmiş ve detay**
   - Tarama geçmişi sayfasını açın.
   - Bir kaydın detayına gidip hedefleri, finding'leri ve scanner adlarını gösterin.

6. **Hata toleransı ve testler**
   - Bir dış servis hata verse bile orchestrator'ın diğer scanner'ları durdurmadığını açıklayın.
   - `dotnet test tests/DigitalFootprintAuditor.UnitTests` komutuyla 25 testin geçtiğini gösterin.

## Hazır cevaplar

- **Neden interface kullanıldı?** Scanner'ların aynı sözleşmeye uymasını ve bağımsız test edilmesini sağlar.
- **Neden `HttpClientFactory`?** HttpClient yaşam döngüsünü doğru yönetir; timeout ve header ayarları merkezi olur.
- **Risk puanı resmi bir standart mı?** Hayır. Eğitim amaçlı, açıklanabilir bir puanlama modelidir.
- **Bir scanner hata verirse ne olur?** Tarama kısmen tamamlanır; diğer scanner sonuçları korunur.
