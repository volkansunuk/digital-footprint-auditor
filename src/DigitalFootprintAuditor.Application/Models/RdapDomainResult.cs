namespace DigitalFootprintAuditor.Application.Models;

public sealed record RdapDomainResult(
    string Domain,
    DateTimeOffset? RegistrationDate,
    DateTimeOffset? UpdatedDate,
    DateTimeOffset? ExpirationDate,
    string? Registrar,
    IReadOnlyCollection<string> Nameservers
);