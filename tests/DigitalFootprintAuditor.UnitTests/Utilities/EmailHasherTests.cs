using DigitalFootprintAuditor.Infrastructure.Utilities;

namespace DigitalFootprintAuditor.UnitTests.Utilities;

public class EmailHasherTests
{
    [Fact]
    public void ComputeSha256Hash_SameEmail_ReturnsSameHash()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var hash1 = EmailHasher.ComputeSha256Hash(email);
        var hash2 = EmailHasher.ComputeSha256Hash(email);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Theory]
    [InlineData("Test@Example.com", "test@example.com")]
    [InlineData("  test@example.com  ", "test@example.com")]
    public void ComputeSha256Hash_DifferentCasingOrWhitespace_ProducesSameHashAsNormalized(string input, string normalizedEquivalent)
    {
        // Act
        var hashFromInput = EmailHasher.ComputeSha256Hash(input);
        var hashFromNormalized = EmailHasher.ComputeSha256Hash(normalizedEquivalent);

        // Assert
        Assert.Equal(hashFromNormalized, hashFromInput);
    }

    [Fact]
    public void ComputeSha256Hash_ReturnsLowercaseHexString()
    {
        // Act
        var hash = EmailHasher.ComputeSha256Hash("test@example.com");

        // Assert
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, hash.ToLowerInvariant());
    }
}