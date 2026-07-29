namespace DigitalFootprintAuditor.Infrastructure.GitHub.Models;
using System.Text.Json.Serialization;

/* public-> bu sınıfı başka namespaceler de kullanabilir, sealed->kimse bu sınıftan miras almasın*/
public sealed class GitHubUserResponse 
{
    [JsonPropertyName("login")] //bu property'nin JSON'daki karşılığı logindir
    public string Login {get; init;} = string.Empty; //init->property sadece oluşturulurken atanır, immutable yaparız
    
    [JsonPropertyName("email")] 
    public string? Email{get; init;} //kullanıcı bu bilgileri paylaşmamış olabileceğinden null gelebilir
    
    [JsonPropertyName("bio")]
    public string? Bio{get; init;}

    [JsonPropertyName("public_repos")] /*anlamı: github jsonunda public_repos alanı varsa 
    değerini PublicRepos propertysine yaz */
    public int PublicRepos{get; init;}

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt{get; init;} //datetime yerine bunu kullanıyrouz çünkü saat dilgisi var

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt{get; init;}
}