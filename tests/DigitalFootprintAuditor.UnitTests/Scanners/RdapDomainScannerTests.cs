using DigitalFootprintAuditor.Infrastructure.Scanners;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class RdapDomainScannerTests
{
    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("EXAMPLE.COM", "example.com")]
    [InlineData(" https://example.com ", "example.com")]
    [InlineData("https://example.com/about", "example.com")]
    [InlineData("http://example.com?test=1", "example.com")]
    public void NormalizeDomain_ShouldReturnHostOnly(
        string input,
        string expected)
    {
        var result = RdapDomainScanner.NormalizeDomain(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a valid domain")]
    public void NormalizeDomain_ShouldThrow_WhenInputIsInvalid(
        string input)
    {
        Assert.Throws<ArgumentException>(() =>
            RdapDomainScanner.NormalizeDomain(input));
    }
}