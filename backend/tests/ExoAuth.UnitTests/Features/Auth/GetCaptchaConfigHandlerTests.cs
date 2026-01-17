using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Queries.GetCaptchaConfig;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class GetCaptchaConfigHandlerTests
{
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly GetCaptchaConfigHandler _handler;

    public GetCaptchaConfigHandlerTests()
    {
        _mockCaptchaService = new Mock<ICaptchaService>();
        _handler = new GetCaptchaConfigHandler(_mockCaptchaService.Object);
    }

    [Fact]
    public async Task Handle_WhenCaptchaEnabled_ReturnsEnabledConfig()
    {
        // Arrange
        var query = new GetCaptchaConfigQuery();
        var expectedConfig = new CaptchaPublicConfig
        {
            Enabled = true,
            Provider = "turnstile",
            SiteKey = "test-site-key"
        };
        _mockCaptchaService.Setup(x => x.GetPublicConfig()).Returns(expectedConfig);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().BeTrue();
        result.Provider.Should().Be("turnstile");
        result.SiteKey.Should().Be("test-site-key");
    }

    [Fact]
    public async Task Handle_WhenCaptchaDisabled_ReturnsDisabledConfig()
    {
        // Arrange
        var query = new GetCaptchaConfigQuery();
        var expectedConfig = new CaptchaPublicConfig
        {
            Enabled = false,
            Provider = null,
            SiteKey = null
        };
        _mockCaptchaService.Setup(x => x.GetPublicConfig()).Returns(expectedConfig);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().BeFalse();
        result.Provider.Should().BeNull();
        result.SiteKey.Should().BeNull();
    }

    [Theory]
    [InlineData("turnstile", "turnstile-site-key")]
    [InlineData("recaptcha", "recaptcha-site-key")]
    [InlineData("hcaptcha", "hcaptcha-site-key")]
    public async Task Handle_ReturnsCorrectProviderConfig(string provider, string siteKey)
    {
        // Arrange
        var query = new GetCaptchaConfigQuery();
        var expectedConfig = new CaptchaPublicConfig
        {
            Enabled = true,
            Provider = provider,
            SiteKey = siteKey
        };
        _mockCaptchaService.Setup(x => x.GetPublicConfig()).Returns(expectedConfig);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Provider.Should().Be(provider);
        result.SiteKey.Should().Be(siteKey);
    }

    [Fact]
    public async Task Handle_CallsCaptchaServiceGetPublicConfig()
    {
        // Arrange
        var query = new GetCaptchaConfigQuery();
        _mockCaptchaService.Setup(x => x.GetPublicConfig()).Returns(new CaptchaPublicConfig());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockCaptchaService.Verify(x => x.GetPublicConfig(), Times.Once);
    }
}
