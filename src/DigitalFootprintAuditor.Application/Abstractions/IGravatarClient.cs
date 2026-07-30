using DigitalFootprintAuditor.Application.Models;

namespace DigitalFootprintAuditor.Application.Abstractions;

//Gravatar API erişimi için abstraction

public interface IGravatarClient
{
    Task<GravatarProfileResult?> GetProfileAsync( 
        string emailHash, 
        CancellationToken cancellationToken);

}