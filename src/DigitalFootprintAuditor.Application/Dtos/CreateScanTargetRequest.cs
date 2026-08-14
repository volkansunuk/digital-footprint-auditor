using System.ComponentModel.DataAnnotations;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.Application.Dtos;

public class CreateScanTargetRequest
{
    public ScanTargetType TargetType { get; set; }

    [Required(ErrorMessage = "TargetValue zorunludur.")]
    [StringLength(500, ErrorMessage = "TargetValue en fazla 500 karakter olabilir.")]
    public string TargetValue { get; set; } = string.Empty;
}