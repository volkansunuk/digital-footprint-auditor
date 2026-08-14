using DigitalFootprintAuditor.Application.Dtos;
using DigitalFootprintAuditor.Application.Validators;
using DigitalFootprintAuditor.Domain.Enums;

namespace DigitalFootprintAuditor.UnitTests.Validators;

public class CreateScanRequestValidatorTests
{
    private readonly CreateScanRequestValidator _validator =
        new(new ScanTargetInputValidator());

    [Fact]
    public async Task Validate_ShouldBeValid_WhenAtLeastOneTargetIsProvided()
    {
        var request = new CreateScanRequestDto(
            [new ScanTargetInputDto(TargetType.GitHubUsername, "octocat")]);

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldBeInvalid_WhenTargetsAreEmpty()
    {
        var request = new CreateScanRequestDto([]);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Targets");
    }

    [Fact]
    public async Task Validate_ShouldBeInvalid_WhenTargetValueIsBlank()
    {
        var request = new CreateScanRequestDto(
            [new ScanTargetInputDto(TargetType.Email, " ")]);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Targets[0].TargetValue");
    }

    [Fact]
    public async Task Validate_ShouldBeInvalid_WhenTargetValueIsTooLong()
    {
        var request = new CreateScanRequestDto(
            [new ScanTargetInputDto(TargetType.Domain, new string('a', 501))]);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Targets[0].TargetValue");
    }
}
