using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class CaptchaServiceTests
{
    private readonly Mock<ICaptchaProvider> _mockProvider;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CaptchaService>> _mockLogger;
    private readonly CaptchaSettings _settings;

    public CaptchaServiceTests()
    {
        _mockProvider = new Mock<ICaptchaProvider>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CaptchaService>>();
        _settings = new CaptchaSettings
        {
            Enabled = true,
            Provider = "Turnstile",
            Turnstile = new TurnstileSettings
            {
                SiteKey = "test-site-key",
                SecretKey = "test-secret-key"
            },
            SmartTrigger = new SmartTriggerSettings
            {
                LoginFailedAttemptsThreshold = 2,
                DeviceApprovalFailedAttemptsThreshold = 2,
                MfaVerifyFailedAttemptsThreshold = 2,
                LoginRiskScoreThreshold = 70
            }
        };
    }

    private CaptchaService CreateService()
    {
        var options = Options.Create(_settings);
        return new CaptchaService(_mockProvider.Object, _mockCacheService.Object, options, _mockLogger.Object);
    }

    #region IsEnabled

    [Fact]
    public void IsEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act & Assert
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act & Assert
        service.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region GetPublicConfig

    [Fact]
    public void GetPublicConfig_WhenEnabled_ReturnsCorrectConfig()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.Provider = "Turnstile";
        _settings.Turnstile.SiteKey = "test-site-key";
        var service = CreateService();

        // Act
        var config = service.GetPublicConfig();

        // Assert
        config.Enabled.Should().BeTrue();
        config.Provider.Should().Be("turnstile");
        config.SiteKey.Should().Be("test-site-key");
    }

    [Fact]
    public void GetPublicConfig_WhenDisabled_ReturnsDisabledConfig()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        var config = service.GetPublicConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.Provider.Should().Be("disabled");
        config.SiteKey.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Turnstile", "test-turnstile-key")]
    [InlineData("Recaptcha", "test-recaptcha-key")]
    [InlineData("HCaptcha", "test-hcaptcha-key")]
    public void GetPublicConfig_ReturnsCorrectSiteKeyForProvider(string provider, string expectedKey)
    {
        // Arrange
        _settings.Enabled = true;
        _settings.Provider = provider;
        _settings.Turnstile.SiteKey = "test-turnstile-key";
        _settings.Recaptcha = new RecaptchaSettings { SiteKey = "test-recaptcha-key" };
        _settings.HCaptcha = new HCaptchaSettings { SiteKey = "test-hcaptcha-key" };
        var service = CreateService();

        // Act
        var config = service.GetPublicConfig();

        // Assert
        config.SiteKey.Should().Be(expectedKey);
    }

    #endregion

    #region ValidateRequiredAsync

    [Fact]
    public async Task ValidateRequiredAsync_WhenDisabled_DoesNotValidate()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        await service.ValidateRequiredAsync(null, "register", null);

        // Assert
        _mockProvider.Verify(x => x.ValidateAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateRequiredAsync_WhenEnabled_WithNullToken_ThrowsCaptchaRequiredException()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act & Assert
        var act = () => service.ValidateRequiredAsync(null, "register", null);
        await act.Should().ThrowAsync<CaptchaRequiredException>();
    }

    [Fact]
    public async Task ValidateRequiredAsync_WhenEnabled_WithEmptyToken_ThrowsCaptchaRequiredException()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act & Assert
        var act = () => service.ValidateRequiredAsync("", "register", null);
        await act.Should().ThrowAsync<CaptchaRequiredException>();
    }

    [Fact]
    public async Task ValidateRequiredAsync_WhenEnabled_WithValidToken_Validates()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockProvider.Setup(x => x.ValidateAsync(
                "valid-token",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CaptchaResult.Successful());

        // Act
        await service.ValidateRequiredAsync("valid-token", "register", null);

        // Assert
        _mockProvider.Verify(x => x.ValidateAsync(
            "valid-token",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateRequiredAsync_WhenEnabled_WithInvalidToken_ThrowsCaptchaInvalidException()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockProvider.Setup(x => x.ValidateAsync(
                "invalid-token",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CaptchaResult.Failed("invalid-input-response"));

        // Act & Assert
        var act = () => service.ValidateRequiredAsync("invalid-token", "register", null);
        await act.Should().ThrowAsync<CaptchaInvalidException>();
    }

    #endregion

    #region ValidateConditionalAsync

    [Fact]
    public async Task ValidateConditionalAsync_WhenNotRequired_DoesNotValidate()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act
        await service.ValidateConditionalAsync(null, false, "login", null);

        // Assert
        _mockProvider.Verify(x => x.ValidateAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateConditionalAsync_WhenRequired_WithNullToken_ThrowsCaptchaRequiredException()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act & Assert
        var act = () => service.ValidateConditionalAsync(null, true, "login", null);
        await act.Should().ThrowAsync<CaptchaRequiredException>();
    }

    [Fact]
    public async Task ValidateConditionalAsync_WhenRequired_WithValidToken_Validates()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockProvider.Setup(x => x.ValidateAsync(
                "valid-token",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CaptchaResult.Successful());

        // Act
        await service.ValidateConditionalAsync("valid-token", true, "login", null);

        // Assert
        _mockProvider.Verify(x => x.ValidateAsync(
            "valid-token",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateConditionalAsync_WhenDisabled_DoesNotValidateEvenIfRequired()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        await service.ValidateConditionalAsync(null, true, "login", null);

        // Assert
        _mockProvider.Verify(x => x.ValidateAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region IsRequiredForLoginAsync

    [Fact]
    public async Task IsRequiredForLoginAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        var result = await service.IsRequiredForLoginAsync("test@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForLoginAsync_WhenNoFailedAttempts_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await service.IsRequiredForLoginAsync("test@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForLoginAsync_WhenFailedAttemptsExceedThreshold_ReturnsTrue()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.SmartTrigger.LoginFailedAttemptsThreshold = 2;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3L);

        // Act
        var result = await service.IsRequiredForLoginAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRequiredForLoginAsync_WhenHighRiskScore_ReturnsTrue()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.SmartTrigger.LoginRiskScoreThreshold = 70;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await service.IsRequiredForLoginAsync("test@example.com", riskScore: 80);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRequiredForLoginAsync_WhenLowRiskScore_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.SmartTrigger.LoginRiskScoreThreshold = 70;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await service.IsRequiredForLoginAsync("test@example.com", riskScore: 50);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsRequiredForDeviceApprovalAsync

    [Fact]
    public async Task IsRequiredForDeviceApprovalAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        var result = await service.IsRequiredForDeviceApprovalAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForDeviceApprovalAsync_WhenNoFailedAttempts_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await service.IsRequiredForDeviceApprovalAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForDeviceApprovalAsync_WhenFailedAttemptsExceedThreshold_ReturnsTrue()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.SmartTrigger.DeviceApprovalFailedAttemptsThreshold = 2;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3L);

        // Act
        var result = await service.IsRequiredForDeviceApprovalAsync(Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsRequiredForMfaVerifyAsync

    [Fact]
    public async Task IsRequiredForMfaVerifyAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = false;
        var service = CreateService();

        // Act
        var result = await service.IsRequiredForMfaVerifyAsync("mfa-token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForMfaVerifyAsync_WhenNoFailedAttempts_ReturnsFalse()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await service.IsRequiredForMfaVerifyAsync("mfa-token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRequiredForMfaVerifyAsync_WhenFailedAttemptsExceedThreshold_ReturnsTrue()
    {
        // Arrange
        _settings.Enabled = true;
        _settings.SmartTrigger.MfaVerifyFailedAttemptsThreshold = 2;
        var service = CreateService();
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3L);

        // Act
        var result = await service.IsRequiredForMfaVerifyAsync("mfa-token");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ValidateRequiredAsync_WhenEnabled_WithWhitespaceToken_ThrowsCaptchaRequiredException()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();

        // Act & Assert
        var act = () => service.ValidateRequiredAsync("   ", "register", null);
        await act.Should().ThrowAsync<CaptchaRequiredException>();
    }

    [Fact]
    public async Task IsRequiredForLoginAsync_CacheKeyIncludesEmail()
    {
        // Arrange
        _settings.Enabled = true;
        var service = CreateService();
        string? capturedKey = null;
        _mockCacheService.Setup(x => x.GetIntegerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .ReturnsAsync(0L);

        // Act
        await service.IsRequiredForLoginAsync("test@example.com");

        // Assert
        capturedKey.Should().NotBeNull();
        capturedKey.Should().Contain("test@example.com");
    }

    #endregion
}
