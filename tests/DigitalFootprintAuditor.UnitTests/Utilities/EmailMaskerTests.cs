using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.UnitTests.Utilities;

public class EmailMaskerTests
{
    [Theory]
    [InlineData("john@test.com", "j**n@test.com")]
    [InlineData("test@example.com", "t**t@example.com")]
    public void Mask_NormalEmail_MasksLocalPart(string input, string expected)
    {
        // Act
        var result = EmailMasker.Mask(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Mask_NoAtSign_ReturnsOriginalUnchanged()
    {
        // Arrange
        var input = "notanemail";

        // Act
        var result = EmailMasker.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Mask_VeryShortLocalPart_ReturnsOriginalUnchanged()
    {
        // Arrange
        var input = "a@test.com";

        // Act
        var result = EmailMasker.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }
}