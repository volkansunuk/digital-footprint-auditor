namespace DigitalFootprintAuditor.UnitTests.Security;

public class EmailHasherTests
{
    [Fact]
    public void Hash_ShouldReturnSameHash_WhenEmailDiffersOnlyByCaseAndWhitespace()
    {
        var firstHash = EmailHasher.Hash("User@example.com");
        var secondHash = EmailHasher.Hash(" user@example.com ");

        Assert.Equal(firstHash, secondHash);
    }
    [Fact]
    public void Hash_ShouldReturnLowercaseSha256Hash_WhenEmailIsValid()
    {
        var hash = EmailHasher.Hash("user@example.com");

        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, hash.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
    [Fact]
    public void Hash_ShouldThrowArgumentException_WhenEmailIsBlank()
    {
        Assert.Throws<ArgumentException>(
            () => EmailHasher.Hash(" "));
    }
}
