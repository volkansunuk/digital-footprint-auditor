namespace DigitalFootprintAuditor.Api.Controllers;
using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

/* burası projenin dış dünyaya açılan kapısı*/

[ApiController] /* .NET' e bu sınıfın bir web api olduğunu söyler. Gelen JSON verilerini 
otomatik olarak C# nesnelerine dönüştürür ve hatalı veri tiplerini otomatik yakalar.*/
[Route("api/[controller]")] /* API'mizin web adresini belirler. [controller] kelimesi 
otomatik olarak sınıf adının başındaki ismi alır. Yani bu endpoint'lerin adresi api/scans olur.*/
public class ScansController : ControllerBase /* İçinde Ok(), NotFound(), CreatedAtAction() gibi 
HTTP cevapları dönmemizi sağlayan temel .NET API özelliklerini barındıran sınıftır.*/
{
    private readonly IScanService _scanService;

    public ScansController(IScanService scanService)
{
    _scanService = scanService;
}
[HttpPost]
public async Task<IActionResult> CreateScan([FromBody] CreateScanRequestDto request)
{
    var result = await _scanService.CreateScanAsync(request);
    return CreatedAtAction(nameof(GetScanById), new { id = result.Id }, result);
}
[HttpGet]
public async Task<IActionResult> GetAllScans()
{
    var results = await _scanService.GetAllScansAsync();
    return Ok(results);
}
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetScanById(Guid id)
{
    var result = await _scanService.GetScanByIdAsync(id);
    if (result == null)
        return NotFound(new { message = $"Scan with ID {id} not found." });

    return Ok(result);
}
[HttpDelete("{id:guid}")]
public async Task<IActionResult> DeleteScan(Guid id)
{
    var deleted = await _scanService.DeleteScanAsync(id);
    if (!deleted)
        return NotFound(new { message = $"Scan with ID {id} not found." });

    return NoContent();
}

} 
