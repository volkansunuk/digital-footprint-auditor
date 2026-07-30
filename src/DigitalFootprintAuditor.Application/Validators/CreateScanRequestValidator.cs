using DigitalFootprintAuditor.Application.Dtos;
using FluentValidation;

namespace DigitalFootprintAuditor.Application.Validators;

public sealed class CreateScanRequestValidator
    : AbstractValidator<CreateScanRequestDto>
{
    public CreateScanRequestValidator(
    IValidator<ScanTargetInputDto> targetValidator)
    {
        RuleFor(request => request.Targets)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("En az bir tarama hedefi gönderilmelidir.");

        RuleForEach(request => request.Targets)
            .SetValidator(targetValidator)
            .When(request => request.Targets is not null);
    }
}
