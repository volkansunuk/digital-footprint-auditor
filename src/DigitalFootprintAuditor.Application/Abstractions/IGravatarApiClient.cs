namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IGravatarApiClient
{
    Task<bool> HasGravatarAsync(string email, CancellationToken cancellationToken);
}