using ExoAuth.Domain.Enums;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ExoAuth.UnitTests.Services;

public sealed class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "this-is-a-very-long-secret-key-for-testing-purposes-minimum-32-chars",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            })
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var userType = UserType.System;
        var permissions = new[] { "system:users:read", "system:users:create" };

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, userType, permissions);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void GenerateAccessToken_CanBeValidated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var userType = UserType.System;
        var permissions = new[] { "system:users:read", "system:users:create" };

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, userType, permissions);
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.UserId.Should().Be(userId);
        claims.Email.Should().Be(email);
        claims.UserType.Should().Be(userType);
        claims.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@test.com";
        var userType = UserType.Organization;
        var permissions = new[] { "org:read", "org:write" };

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, userType, permissions);
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.UserId.Should().Be(userId);
        claims.Email.Should().Be(email);
        claims.UserType.Should().Be(UserType.Organization);
        claims.Permissions.Should().Contain("org:read");
        claims.Permissions.Should().Contain("org:write");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64UrlSafeString()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotEndWith("=");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateAccessToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = _tokenService.ValidateAccessToken(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_WithExpiredToken_ReturnsNull()
    {
        // Arrange - create service with very short expiration
        var shortLivedConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "this-is-a-very-long-secret-key-for-testing-purposes-minimum-32-chars",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "0", // Immediate expiration
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            })
            .Build();

        var shortLivedService = new TokenService(shortLivedConfig);
        var token = shortLivedService.GenerateAccessToken(Guid.NewGuid(), "test@test.com", UserType.System, Array.Empty<string>());

        // Wait a moment for token to expire
        Thread.Sleep(100);

        // Act
        var result = shortLivedService.ValidateAccessToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_WithWrongSecret_ReturnsNull()
    {
        // Arrange
        var differentSecretConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "completely-different-secret-key-that-is-also-very-long-here",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            })
            .Build();

        var otherService = new TokenService(differentSecretConfig);

        // Generate token with original service
        var token = _tokenService.GenerateAccessToken(Guid.NewGuid(), "test@test.com", UserType.System, Array.Empty<string>());

        // Act - try to validate with different secret
        var result = otherService.ValidateAccessToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AccessTokenExpiration_ReturnsConfiguredValue()
    {
        // Assert
        _tokenService.AccessTokenExpiration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void RefreshTokenExpiration_ReturnsConfiguredValue()
    {
        // Assert
        _tokenService.RefreshTokenExpiration.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void GenerateAccessToken_WithEmptyPermissions_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, UserType.System, Array.Empty<string>());
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void GenerateAccessToken_WithManyPermissions_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var permissions = Enumerable.Range(1, 50).Select(i => $"permission:{i}").ToArray();

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, UserType.System, permissions);
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.Permissions.Should().HaveCount(50);
    }

    [Theory]
    [InlineData(UserType.System)]
    [InlineData(UserType.Organization)]
    [InlineData(UserType.Project)]
    public void GenerateAccessToken_CorrectlyEncodesUserType(UserType userType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, userType, Array.Empty<string>());
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.UserType.Should().Be(userType);
    }

    [Fact]
    public void Constructor_ThrowsWhenSecretNotConfigured()
    {
        // Arrange
        var configWithoutSecret = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TestIssuer"
            })
            .Build();

        // Act & Assert
        var act = () => new TokenService(configWithoutSecret);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Secret*");
    }

    [Fact]
    public void ValidateAccessToken_TokenHasCorrectExpirationTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateAccessToken(userId, email, UserType.System, Array.Empty<string>());
        var afterGeneration = DateTime.UtcNow;
        var claims = _tokenService.ValidateAccessToken(token);

        // Assert
        claims.Should().NotBeNull();
        var expectedExpiration = beforeGeneration.Add(_tokenService.AccessTokenExpiration);
        claims!.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(2));
    }
}
