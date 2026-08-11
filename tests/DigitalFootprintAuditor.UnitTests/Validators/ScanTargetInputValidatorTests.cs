using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Application.Validators;
using DigitalFootprintAuditor.Domain.Enums;
using FluentValidation.TestHelper;

namespace DigitalFootprintAuditor.UnitTests.Validators;

public class ScanTargetInputValidatorTests
{
    private readonly ScanTargetInputValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveError_WhenTargetIsValid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Email,
            "test@example.com");

        var result = _validator.TestValidate(target);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTargetValueIsEmpty()
    {
        var target = new ScanTargetInputDto(
            TargetType.Email,
            string.Empty);

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTargetValueExceedsMaximumLength()
    {
        var target = new ScanTargetInputDto(
            TargetType.Domain,
            new string('a', 501));

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTargetTypeIsInvalid()
    {
        var target = new ScanTargetInputDto(
            (TargetType)999,
            "example.com");

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetType);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Email,
            "invalid-email");

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenEmailIsValid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Email,
            "test@example.com");

        var result = _validator.TestValidate(target);

        result.ShouldNotHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenGitHubUsernameContainsInvalidCharacters()
    {
        var target = new ScanTargetInputDto(
            TargetType.GitHubUsername,
            "user name!");

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenGitHubUsernameIsValid()
    {
        var target = new ScanTargetInputDto(
            TargetType.GitHubUsername,
            "sample-user");

        var result = _validator.TestValidate(target);

        result.ShouldNotHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDomainIsInvalid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Domain,
            "invalid domain");

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenDomainIsValid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Domain,
            "example.com");

        var result = _validator.TestValidate(target);

        result.ShouldNotHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenWebsiteSchemeIsInvalid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Website,
            "ftp://example.com");

        var result = _validator.TestValidate(target);

        result.ShouldHaveValidationErrorFor(
            x => x.TargetValue);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenWebsiteIsValid()
    {
        var target = new ScanTargetInputDto(
            TargetType.Website,
            "https://example.com");

        var result = _validator.TestValidate(target);

        result.ShouldNotHaveValidationErrorFor(
            x => x.TargetValue);
    }
}