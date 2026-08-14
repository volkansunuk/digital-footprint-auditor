using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalFootprintAuditor.Api.Pages;

public class NewScanModel : PageModel
{
    private readonly IScanService _scanService;

    public NewScanModel(IScanService scanService)
    {
        _scanService = scanService;
    }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? GitHubUsername { get; set; }

    [BindProperty]
    public string? Domain { get; set; }

    [BindProperty]
    public string? Website { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        var targets = new List<ScanTargetInputDto>();

        if (!string.IsNullOrWhiteSpace(Email))
        {
            targets.Add(new ScanTargetInputDto(
                TargetType.Email,
                Email.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(GitHubUsername))
        {
            targets.Add(new ScanTargetInputDto(
                TargetType.GitHubUsername,
                GitHubUsername.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(Domain))
        {
            targets.Add(new ScanTargetInputDto(
                TargetType.Domain,
                Domain.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(Website))
        {
            targets.Add(new ScanTargetInputDto(
                TargetType.Website,
                Website.Trim()));
        }

        if (targets.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tarama başlatmak için en az bir hedef giriniz.");

            return Page();
        }

        var request = new CreateScanRequestDto(targets);

        var scanId = await _scanService.PrepareScanAsync(
            request,
            cancellationToken);

        return RedirectToPage(
            "/ScanDetail",
            new
            {
                id = scanId,
                start = true
            });
    }
}