using ExoAuth.Infrastructure.Services.Captcha;
using FluentAssertions;

namespace ExoAuth.UnitTests.Services.Captcha;

public sealed class DisabledCaptchaProviderTests
{
    [Fact]
    public void ProviderName_ReturnsDisabled()
    {
        // Arrange
        var provider = new DisabledCaptchaProvider();

        // Assert
        provider.ProviderName.Should().Be("disabled");
    }

    [Fact]
    public async Task ValidateAsync_AlwaysReturnsSuccess()
    {
        // Arrange
        var provider = new DisabledCaptchaProvider();

        // Act
        var result = await provider.ValidateAsync("any-token", "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithNullToken_ReturnsSuccess()
    {
        // Arrange
        var provider = new DisabledCaptchaProvider();

        // Act
        var result = await provider.ValidateAsync(null!, null);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyToken_ReturnsSuccess()
    {
        // Arrange
        var provider = new DisabledCaptchaProvider();

        // Act
        var result = await provider.ValidateAsync("", null);

        // Assert
        result.Success.Should().BeTrue();
    }
}
