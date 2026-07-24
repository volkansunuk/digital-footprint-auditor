namespace DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;

/* burada bir interface oluşturduk çünkü Katmanlı mimaride (Clean Architecture) API katmanının,
 veritabanı veya iş mantığının arkada tam olarak nasıl çalıştığını bilmesini istemeyiz.*/ 
public interface IScanService
{
    Task<ScanResponseDto> CreateScanAsync(CreateScanRequestDto request); /* scan oluşturulacak, Dışarıdan 
    bir istek gelecek ve bir tarama başlatılacak*/
    Task<ScanResponseDto?> GetScanByIdAsync(Guid scanId); /* detay endpoint'i, "Şu ID'li taramanın sonucu ver" diyecek, 
    burada ayrıca ? yani nullable kullanmamızın nedeni kullanıcı veritabanında var olmayan bir id gönderirse
     API bunu yakalayıp 404 not found döndürebilsin*/
    Task<IEnumerable<ScanResponseDto>> GetAllScansAsync(); /* listeleme, Sistemdeki tüm yapılmış taramalar çekilmek
     istenecek / burada dönüş tipi olarak task seçtik bu asenkrondur ve Veritabanı okuma/yazma veya dış API'lere
      istek atma işlemleri milisaniyeler de sürse zaman alır. Sunucuyu kilitlediğimiz senaryoların önüne geçmek
       için C#'ta I/O (Girdi/Çıktı) gerektiren tüm operasyonlar asenkron (async/await) yazılır. 
       Task kullanmamızın sebebi budur.*/
    Task<bool> DeleteScanAsync(Guid scanId); /* silme endpoint'i, kullanıcı belirttiği id'li taramayı silmek isteyecek. 
    bool dönüyoruz çünkü silme başarılı mı yoksa böyle bir id yok mu bunu ayırt etmemiz lazım (API bunu 204 veya 404'e çevirecek) */
    
}