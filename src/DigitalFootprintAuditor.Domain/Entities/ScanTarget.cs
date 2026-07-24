namespace DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
public class ScanTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScanId {get; set;} /*yeni bir scan oluşturulduğunda bu scanin idsi ile 
    eşleşecek şekilde scanid oluşturulacak yani scan ile scan target arasında ilişki kurulacak*/
    public TargetType TargetType {get; set;}
    public string TargetValue {get; set;} = string.Empty; //target value boş olamaz çünkü scan edilecek değer bu olacak ben "example.com" gibi bir değer atayacağım
}