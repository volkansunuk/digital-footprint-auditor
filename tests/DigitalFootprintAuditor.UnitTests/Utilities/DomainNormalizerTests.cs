using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.UnitTests.Utilities;

public class DomainNormalizerTests
{
    [Theory]
    [InlineData("https://Example.com/", "example.com")]
    [InlineData("http://www.example.com", "example.com")]
    [InlineData("WWW.EXAMPLE.COM", "example.com")]
    [InlineData("example.com", "example.com")]
    public void Normalize_VariousFormats_ReturnsCleanDomain(string input, string expected)
    {
        // Act
        var result = DomainNormalizer.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}