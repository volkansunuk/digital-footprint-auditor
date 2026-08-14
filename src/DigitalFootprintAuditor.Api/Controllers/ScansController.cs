using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DigitalFootprintAuditor.Api.Controllers;

[ApiController] 
[Route("api/[controller]")] 
public class ScansController : ControllerBase
{
    private readonly IScanService _scanService;
    private readonly IValidator<CreateScanRequestDto> _createScanRequestValidator;

    public ScansController(
        IScanService scanService,
        IValidator<CreateScanRequestDto> createScanRequestValidator)
    {
        _scanService = scanService;
        _createScanRequestValidator = createScanRequestValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateScan([FromBody] CreateScanRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _createScanRequestValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var result = await _scanService.CreateScanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetScanById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllScans(CancellationToken cancellationToken)
    {
        var results = await _scanService.GetAllScansAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetScanByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(new { message = $"Scan with ID {id} not found." });

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteScan(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _scanService.DeleteScanAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"Scan with ID {id} not found." });

        return NoContent();
    }
}
