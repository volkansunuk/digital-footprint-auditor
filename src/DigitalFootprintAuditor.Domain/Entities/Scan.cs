namespace DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
public class Scan
{
    public Guid Id {get; set;} = Guid.NewGuid(); /*guid c# ta benzersiz kimlikler oluşturmak için kullanılır. 
    NewGuid() ile yeni bir benzersiz kimlik oluşturuyoruz.*/
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow; //tarihler için datetime kullanırız
    public DateTime? CompletedAt {get; set;} /* ? bu işaret ile nullable yani ilk başladığında boş 
    olacak çünkü daha tamamlanmadı */
    public ScanStatus Status {get; set;} = ScanStatus.Pending; /*scanstatus enumunu kullanarak 
    default olarak pending atadık(yazım hatalarına karşı korur) */
    public int RiskScore {get; set;}
    public RiskLevel RiskLevel {get; set;}

/*aşağıdakileri ekledik çünkü scan ile target ve finding arasında bire çok ilişki var. 
yani bir scan birden fazla target ve finding içerebilir. 
bu yüzden ICollection kullandık. bu sayede bir scan nesnesi oluşturulduğunda, 
Targets ve Findings koleksiyonları da otomatik olarak boş listelerle başlatılır.
*/ 
    public ICollection<ScanTarget> Targets { get; set; } = new List<ScanTarget>(); 
    public ICollection<ScanFinding> Findings { get; set; } = new List<ScanFinding>();
}