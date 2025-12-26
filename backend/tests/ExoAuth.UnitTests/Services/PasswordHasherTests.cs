using ExoAuth.Infrastructure.Services;
using FluentAssertions;

namespace ExoAuth.UnitTests.Services;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void Hash_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordHasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().Contain("$"); // Format: salt$hash
    }

    [Fact]
    public void Hash_ReturnsDifferentHashForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.Hash(password);
        var hash2 = _passwordHasher.Hash(password);

        // Assert
        hash1.Should().NotBe(hash2); // Different salts should produce different hashes
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var malformedHash = "not-a-valid-hash";

        // Act
        var result = _passwordHasher.Verify(password, malformedHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyHash_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasher.Verify(password, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithHashMissingSeparator_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "noDollarSignInHash";

        // Act
        var result = _passwordHasher.Verify(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesBase64EncodedParts()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordHasher.Hash(password);
        var parts = hash.Split('$');

        // Assert
        parts.Should().HaveCount(2);

        // Both parts should be valid base64
        var salt = Convert.FromBase64String(parts[0]);
        var hashBytes = Convert.FromBase64String(parts[1]);

        salt.Should().HaveCount(16); // SaltSize
        hashBytes.Should().HaveCount(32); // HashSize
    }

    [Theory]
    [InlineData("short")]
    [InlineData("averagepassword")]
    [InlineData("VeryLongPasswordThatIsMoreThan50CharactersLong1234567890!@#$%")]
    public void Hash_WorksWithVariousPasswordLengths(string password)
    {
        // Act
        var hash = _passwordHasher.Hash(password);
        var result = _passwordHasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Hash_WorksWithWhitespacePassword()
    {
        // Arrange
        var password = "   ";

        // Act
        var hash = _passwordHasher.Hash(password);
        var result = _passwordHasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify("testpassword123!", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_DetectsTamperedHash()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordHasher.Hash(password);

        // Tamper with the hash part
        var parts = hash.Split('$');
        var tamperedHash = parts[0] + "$" + "AAAA" + parts[1].Substring(4);

        // Act
        var result = _passwordHasher.Verify(password, tamperedHash);

        // Assert
        result.Should().BeFalse();
    }
}
