using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Domain.Enums;
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
            .MaximumLength(500)
            .WithMessage("Hedef değeri 500 karakterden uzun olamaz.");
    
        When(
            target => target.TargetType == TargetType.Email,
            () =>
            {
                RuleFor(target => target.TargetValue)
                    .EmailAddress()
                    .WithMessage(
                        "Geçerli bir e-posta adresi gönderilmelidir.");
            });

        When(
            target => target.TargetType == TargetType.GitHubUsername,
            () =>
            {
                RuleFor(target => target.TargetValue)
                    .Matches("^[a-zA-Z0-9-]+$")
                    .WithMessage(
                        "GitHub kullanıcı adı yalnızca harf, rakam ve tire içerebilir.");
            });

        When(
            target => target.TargetType == TargetType.Website,
            () =>
            {
                RuleFor(target => target.TargetValue)
                    .Must(value =>
                        Uri.TryCreate(
                            value,
                            UriKind.Absolute,
                            out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp ||
                        uri.Scheme == Uri.UriSchemeHttps))
                    .WithMessage(
                        "Website değeri geçerli bir HTTP veya HTTPS adresi olmalıdır.");
            });

        When(
            target => target.TargetType == TargetType.Domain,
            () =>
            {
                RuleFor(target => target.TargetValue)
                    .Must(value =>
                        !string.IsNullOrWhiteSpace(value) &&
                        value.Contains('.') &&
                        !value.Contains(' '))
                    .WithMessage(
                        "Geçerli bir domain değeri gönderilmelidir.");
            });
    }
}
