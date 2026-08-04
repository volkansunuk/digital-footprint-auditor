namespace DigitalFootprintAuditor.Application.Models;

public sealed record DnsRecordResult(
    IReadOnlyCollection<string> ARecords,
    IReadOnlyCollection<string> AaaaRecords,
    IReadOnlyCollection<string> MxRecords,
    IReadOnlyCollection<string> TxtRecords,
    IReadOnlyCollection<string> DmarcRecords
);



