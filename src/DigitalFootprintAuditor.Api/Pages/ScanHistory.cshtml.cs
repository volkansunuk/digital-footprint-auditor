using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalFootprintAuditor.Api.Pages;

public class ScanHistoryModel : PageModel
{
    private readonly IScanService _scanService;

    public ScanHistoryModel(IScanService scanService)
    {
        _scanService = scanService;
    }

    public IReadOnlyCollection<ScanResponseDto> Scans { get; private set; }
        = Array.Empty<ScanResponseDto>();

    public async Task OnGetAsync(
        CancellationToken cancellationToken)
    {
        Scans = await _scanService.GetAllScansAsync(
            cancellationToken);
    }
}