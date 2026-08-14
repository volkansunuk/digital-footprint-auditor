namespace DigitalFootprintAuditor.Application.Abstractions;

public class RdapDomainResult
{
    public DateTime? RegistrationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? LastChangedDate { get; set; }
}

public interface IRdapApiClient
{
    Task<RdapDomainResult?> GetDomainInfoAsync(string domain, CancellationToken cancellationToken);
}