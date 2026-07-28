using System.Net.Http.Json;
using DigitalFootprintAuditor.Infrastructure.GitHub.Models;

namespace DigitalFootprintAuditor.Infrastructure.GitHub;

public sealed class GitHubClient //sealed:bu sınıftan miras alınmaz
{
    private readonly HttpClient _httpClient; //private:bu alanı sadece GitHubClient kullanır, 
    //readonly: tanımlandığı yere atanabilir, düzenleme yapılamaz
    public GitHubClient(HttpClient httpClient)//GitHubClient, ihtiyaç duyduğu HttpClient nesnesini constructor üzerinden alır.
    {
        _httpClient = httpClient; //Constructordan gelen HttpClient, sınıfın private field’ına atanır. diğer metotlar da kullanabilir.
    }

    public async Task<GitHubUserResponse?> GetUserAsync(string username, CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<GitHubUserResponse>(
            $"users/{username}",
            cancellationToken);
    }
}

