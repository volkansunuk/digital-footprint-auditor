using DigitalFootprintAuditor.Application.Models;
namespace DigitalFootprintAuditor.Application.Abstractions;

public interface IGravatarClient
{
    Task<GravatarProfileResult?> GetProfileAsync( 
        string emailHash, 
        CancellationToken cancellationToken);
}