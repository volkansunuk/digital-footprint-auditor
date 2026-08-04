using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DnsClient;

namespace DigitalFootprintAuditor.Infrastructure.Dns;

public sealed class DnsClient : IDnsClient
{
    private readonly LookupClient _lookupClient;

    public DnsClient(LookupClient lookupClient)
    {
        _lookupClient = lookupClient;
    }

    public async Task<DnsRecordResult> GetRecordsAsync(
        string domain,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);

        var normalizedDomain = domain.Trim().TrimEnd('.');

        var aResponse = await _lookupClient.QueryAsync(
            normalizedDomain,
            QueryType.A,
            QueryClass.IN,
            cancellationToken);

        var aaaaResponse = await _lookupClient.QueryAsync(
            normalizedDomain,
            QueryType.AAAA,
            QueryClass.IN,
            cancellationToken);

        var mxResponse = await _lookupClient.QueryAsync(
            normalizedDomain,
            QueryType.MX,
            QueryClass.IN,
            cancellationToken);

        var txtResponse = await _lookupClient.QueryAsync(
            normalizedDomain,
            QueryType.TXT,
            QueryClass.IN,
            cancellationToken);

        var dmarcResponse = await _lookupClient.QueryAsync(
            $"_dmarc.{normalizedDomain}",
            QueryType.TXT,
            QueryClass.IN,
            cancellationToken);

        var aRecords = aResponse.Answers
            .ARecords()
            .Select(record => record.Address.ToString())
            .ToArray();

        var aaaaRecords = aaaaResponse.Answers
            .AaaaRecords()
            .Select(record => record.Address.ToString())
            .ToArray();

        var mxRecords = mxResponse.Answers
            .MxRecords()
            .Select(record => record.Exchange.Value)
            .ToArray();

        var txtRecords = txtResponse.Answers
            .TxtRecords()
            .Select(record => string.Concat(record.Text))
            .ToArray();

        var dmarcRecords = dmarcResponse.Answers
            .TxtRecords()
            .Select(record => string.Concat(record.Text))
            .ToArray();

        return new DnsRecordResult(
            ARecords: aRecords,
            AaaaRecords: aaaaRecords,
            MxRecords: mxRecords,
            TxtRecords: txtRecords,
            DmarcRecords: dmarcRecords);
    }
}