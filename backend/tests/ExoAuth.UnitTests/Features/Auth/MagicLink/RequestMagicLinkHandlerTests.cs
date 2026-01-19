using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.MagicLink;

public sealed class RequestMagicLinkHandlerTests
{
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IMagicLinkService> _mockMagicLinkService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly Mock<ILogger<RequestMagicLinkHandler>> _mockLogger;
    private readonly RequestMagicLinkHandler _handler;

    public RequestMagicLinkHandlerTests()
    {
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockMagicLinkService = new Mock<IMagicLinkService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockLogger = new Mock<ILogger<RequestMagicLinkHandler>>();

        // Default CAPTCHA service setup - always valid in tests
        _mockCaptchaService.Setup(x => x.ValidateRequiredAsync(
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RequestMagicLinkHandler(
            _mockUserRepository.Object,
            _mockMagicLinkService.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockCaptchaService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_CreatesMagicLinkAndSendsEmail()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@example.com", "captcha-token");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        var magicLinkResult = new MagicLinkResult(magicLinkToken, "test-token-value");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockMagicLinkService.Setup(x => x.CreateMagicLinkAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockMagicLinkService.Verify(x => x.CreateMagicLinkAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendMagicLinkAsync(
            user.Email,
            user.FirstName,
            "test-token-value",
            user.Id,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.MagicLinkRequested,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("notfound@example.com", "captcha-token");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns success to prevent email enumeration
        result.Success.Should().BeTrue();
        _mockMagicLinkService.Verify(x => x.CreateMagicLinkAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(x => x.SendMagicLinkAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("inactive@example.com", "captcha-token");
        var user = TestDataFactory.CreateSystemUser(email: "inactive@example.com");
        user.Deactivate();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns success to prevent user enumeration
        result.Success.Should().BeTrue();
        _mockMagicLinkService.Verify(x => x.CreateMagicLinkAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(x => x.SendMagicLinkAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAnonymizedUser_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("anonymized@example.com", "captcha-token");
        var user = TestDataFactory.CreateSystemUser(email: "anonymized@example.com");
        user.Anonymize();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns success to prevent user enumeration
        result.Success.Should().BeTrue();
        _mockMagicLinkService.Verify(x => x.CreateMagicLinkAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(x => x.SendMagicLinkAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("TEST@EXAMPLE.COM", "captcha-token");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectMessage()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@example.com", "captcha-token");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Message.Should().Contain("If an account exists with this email");
    }

    [Fact]
    public async Task Handle_ValidatesCaptcha()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@example.com", "captcha-token", "192.168.1.1");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCaptchaService.Verify(x => x.ValidateRequiredAsync(
            "captcha-token",
            "magic_link",
            "192.168.1.1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesUserPreferredLanguageForEmail()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@example.com", "captcha-token");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        user.SetPreferredLanguage("de-DE");
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        var magicLinkResult = new MagicLinkResult(magicLinkToken, "test-token-value");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockMagicLinkService.Setup(x => x.CreateMagicLinkAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendMagicLinkAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            "de-DE",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
