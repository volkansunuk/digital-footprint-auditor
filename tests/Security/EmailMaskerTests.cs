namespace DigitalFootprintAuditor.UnitTests.Security;

public class EmailMaskerTests
{
    [Fact]
    public void Mask_ShouldHideLocalPartExceptFirstCharacter_WhenEmailIsValid()
    {
        var result = EmailMasker.Mask("user@example.com");

        Assert.Equal("u***@example.com", result);
    }
    [Fact]
    public void Mask_ShouldTrimWhiteSpace_WhenEmailContainsWhitespace()
    {
        var result = EmailMasker.Mask(" user@example.com ");

        Assert.Equal("u***@example.com", result);
    }
    [Fact]
    public void Mask_ShouldThrowArgumentException_WhenEmailIsBlank()
    {
        Assert.Throws<ArgumentException>(
            () => EmailMasker.Mask(" "));
    }
    [Fact]
    public void Mask_ShouldThrowArgumentException_WhenEmailHasNoDomain()
    {
        Assert.Throws<ArgumentException>(
            () => EmailMasker.Mask("user@"));
    }
    [Fact]
    public void Mask_ShouldThrowArgumentException_WhenEmailHasNoLocalPart()
    {
        Assert.Throws<ArgumentException>(
            () => EmailMasker.Mask("@example.com"));
    }
}