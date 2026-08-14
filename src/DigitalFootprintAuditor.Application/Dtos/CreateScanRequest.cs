using System.ComponentModel.DataAnnotations;

namespace DigitalFootprintAuditor.Application.Dtos;

public class CreateScanRequest
{
    [Required(ErrorMessage = "En az bir hedef belirtilmelidir.")]
    [MinLength(1, ErrorMessage = "En az bir hedef belirtilmelidir.")]
    public List<CreateScanTargetRequest> Targets { get; set; } = new();
}