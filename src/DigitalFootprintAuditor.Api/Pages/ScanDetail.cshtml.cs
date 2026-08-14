using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalFootprintAuditor.Api.Pages;

public class ScanDetailModel : PageModel
{
    private readonly IScanService _scanService;

    public ScanDetailModel(IScanService scanService)
    {
        _scanService = scanService;
    }

    public ScanResponseDto? Scan { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return Page();
        }

        Scan = await _scanService.GetScanByIdAsync(
            id.Value,
            cancellationToken);

        if (Scan is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartScanAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _scanService.RunScanAsync(
            id,
            cancellationToken);

        return new JsonResult(new
        {
            success = true
        });
    }
    
}