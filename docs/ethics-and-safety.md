# Etik ve Güvenlik Sınırları

Digital Footprint Auditor **eğitim amaçlı, savunmacı ve kullanıcı onayına dayalı** bir uygulamadır.
Amacı, kullanıcının **kendi** dijital ayak izini görmesi ve iyileştirmesidir.

## İzin verilen kullanım

Uygulama yalnızca şunlar üzerinde kullanılır:

- Kullanıcının kendisine ait olduğunu beyan ettiği e-posta adresleri
- Kullanıcının kendi GitHub hesapları
- Kullanıcının sahip olduğu veya yönetmeye yetkili olduğu domainler
- Herkese açık ve izinli API kaynakları (GitHub public API, Gravatar, RDAP, public DNS)

## Kapsam dışı — asla eklenmeyecek özellikler

- Telefon numarasından kişi araştırma
- Ad ve soyad üzerinden kişi bulma
- Sosyal medya hesaplarını toplu şekilde arama
- Gizli profillere erişme
- Giriş gerektiren sayfalardan veri çekme
- Yüz tanıma
- Sızdırılmış şifreleri gösterme
- Veri ihlallerindeki hassas verileri görüntüleme (yalnızca "ihlalde geçiyor / geçmiyor" durumu gösterilebilir)
- Toplu e-posta veya kişi taraması
- Başka kişilere ait dijital profilleri izinsiz analiz etme
- Platformların kullanım koşullarını ihlal eden scraping işlemleri
- Port taraması, zafiyet taraması, exploit veya saldırı simülasyonu

## Geliştirici sorumlulukları

- Test ederken **kendi** hesaplarınızı ve **kendi** (veya şirketin izin verdiği) domainleri kullanın.
- E-posta gibi kişisel verileri loglara açık yazmayın; maskeleyin (`vol***@example.com`).
- Harici servislerin rate limit ve kullanım koşullarına uyun.
- API anahtarlarını repository'ye eklemeyin.

Bu sınırlar tartışmaya açık değildir. Bir özellik fikrinin bu listeye takılıp takılmadığından emin
değilseniz, geliştirmeye başlamadan önce staj sorumlusuna danışın.
