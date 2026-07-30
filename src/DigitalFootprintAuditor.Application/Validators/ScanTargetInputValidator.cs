using DigitalFootprintAuditor.Application.Dtos;
using FluentValidation;

namespace DigitalFootprintAuditor.Application.Validators;

public sealed class ScanTargetInputValidator
    : AbstractValidator<ScanTargetInputDto>
{
    public ScanTargetInputValidator()
    {
        RuleFor(target => target.TargetType)
            .IsInEnum()
            .WithMessage("Geçerli bir hedef türü gönderilmelidir.");

        RuleFor(target => target.TargetValue)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Hedef değeri boş bırakılamaz.")
            .WithMessage("Hedef değeri boş bırakılamaz.")
            .MaximumLength(500)
            .WithMessage("Hedef değeri 500 karakterden uzun olamaz.");
    }
}
