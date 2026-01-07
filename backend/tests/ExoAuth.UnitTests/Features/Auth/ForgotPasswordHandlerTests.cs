using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.ForgotPassword;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class ForgotPasswordHandlerTests
{
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordResetService> _mockPasswordResetService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly Mock<ILogger<ForgotPasswordHandler>> _mockLogger;
    private readonly ForgotPasswordHandler _handler;

    public ForgotPasswordHandlerTests()
    {
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordResetService = new Mock<IPasswordResetService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockLogger = new Mock<ILogger<ForgotPasswordHandler>>();

        // Default CAPTCHA service setup - always valid in tests
        _mockCaptchaService.Setup(x => x.ValidateRequiredAsync(
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ForgotPasswordHandler(
            _mockUserRepository.Object,
            _mockPasswordResetService.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockCaptchaService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_SendsResetEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand("test@example.com");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var resetResult = new PasswordResetResult(
            PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH"),
            "token123",
            "ABCD-EFGH"
        );

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordResetService.Setup(x => x.CreateResetTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPasswordResetService.Verify(x => x.CreateResetTokenAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            user.Email,
            user.FirstName,
            "token123",
            "ABCD-EFGH",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasswordResetRequested,
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
        var command = new ForgotPasswordCommand("notfound@example.com");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns success to prevent email enumeration
        result.Success.Should().BeTrue();
        _mockPasswordResetService.Verify(x => x.CreateResetTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand("inactive@example.com");
        var user = TestDataFactory.CreateSystemUser(email: "inactive@example.com");
        user.Deactivate();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns success to prevent user enumeration
        result.Success.Should().BeTrue();
        _mockPasswordResetService.Verify(x => x.CreateResetTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var command = new ForgotPasswordCommand("TEST@EXAMPLE.COM");

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
        var command = new ForgotPasswordCommand("test@example.com");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Message.Should().Contain("If an account exists with this email");
    }
}
