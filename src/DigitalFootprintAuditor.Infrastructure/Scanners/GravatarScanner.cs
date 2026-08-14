using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.Infrastructure.Scanners;

public class GravatarScanner : IFootprintScanner
{
    private readonly IGravatarApiClient _gravatarApiClient;

    public GravatarScanner(IGravatarApiClient gravatarApiClient)
    {
        _gravatarApiClient = gravatarApiClient;
    }

    public ScanTargetType SupportedTargetType => ScanTargetType.Email;

    public async Task<List<ScanFindingResult>> ScanAsync(string targetValue, CancellationToken cancellationToken)
    {
        var findings = new List<ScanFindingResult>();
        var maskedEmail = EmailMasker.Mask(targetValue);

        bool hasGravatar;

        try
        {
            hasGravatar = await _gravatarApiClient.HasGravatarAsync(targetValue, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GravatarScanner),
                Title = "Gravatar taraması başarısız oldu",
                Description = $"Gravatar API'sine bağlanırken bir hata oluştu: {ex.Message}",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "Gravatar API"
            });
            return findings;
        }

        if (hasGravatar)
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GravatarScanner),
                Title = "E-posta adresi Gravatar'da kayıtlı",
                Description = $"{maskedEmail} adresi, Gravatar üzerinde herkese açık bir profil resmine sahip. Bu, e-postanın çeşitli platformlarda kullanıldığına dair bir sinyal olabilir.",
                Severity = FindingSeverity.Low,
                ScoreImpact = 5,
                Source = "Gravatar API"
            });
        }
        else
        {
            findings.Add(new ScanFindingResult
            {
                ScannerName = nameof(GravatarScanner),
                Title = "Gravatar kaydı bulunamadı",
                Description = $"{maskedEmail} adresi için Gravatar'da kayıtlı bir profil bulunamadı.",
                Severity = FindingSeverity.Info,
                ScoreImpact = 0,
                Source = "Gravatar API"
            });
        }

        return findings;
    }
}