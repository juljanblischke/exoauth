using ExoAuth.Domain.Entities;
using FluentAssertions;

namespace ExoAuth.UnitTests.Features.Auth.MagicLink;

public sealed class MagicLinkTokenTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var expirationMinutes = 15;

        // Act
        var magicLinkToken = MagicLinkToken.Create(userId, token, expirationMinutes);

        // Assert
        magicLinkToken.UserId.Should().Be(userId);
        magicLinkToken.TokenHash.Should().NotBeNullOrEmpty();
        magicLinkToken.IsUsed.Should().BeFalse();
        magicLinkToken.UsedAt.Should().BeNull();
        magicLinkToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(expirationMinutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_HashesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-token-value";

        // Act
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Assert
        magicLinkToken.TokenHash.Should().NotBe(token);
        magicLinkToken.TokenHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = MagicLinkToken.GenerateToken();
        var token2 = MagicLinkToken.GenerateToken();

        // Assert
        token1.Should().NotBe(token2);
        token1.Should().HaveLength(43); // Base64 of 32 bytes without padding
    }

    [Fact]
    public void GenerateToken_ReturnsUrlSafeToken()
    {
        // Act
        var token = MagicLinkToken.GenerateToken();

        // Assert
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Act & Assert
        magicLinkToken.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, -1); // Already expired

        // Act & Assert
        magicLinkToken.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenNotUsedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Act & Assert
        magicLinkToken.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenUsed_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);
        magicLinkToken.MarkAsUsed();

        // Act & Assert
        magicLinkToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, -1);

        // Act & Assert
        magicLinkToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_SetsIsUsedAndUsedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Act
        magicLinkToken.MarkAsUsed();

        // Assert
        magicLinkToken.IsUsed.Should().BeTrue();
        magicLinkToken.UsedAt.Should().NotBeNull();
        magicLinkToken.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateToken_WithCorrectToken_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-token-value";
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Act
        var result = magicLinkToken.ValidateToken(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithIncorrectToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-token-value";
        var magicLinkToken = MagicLinkToken.Create(userId, token, 15);

        // Act
        var result = magicLinkToken.ValidateToken("wrong-token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Create_WithDifferentExpirationMinutes_SetsCorrectExpiry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = MagicLinkToken.GenerateToken();

        // Act
        var magicLinkToken5 = MagicLinkToken.Create(userId, token, 5);
        var magicLinkToken30 = MagicLinkToken.Create(userId, token, 30);

        // Assert
        magicLinkToken5.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
        magicLinkToken30.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }
}
