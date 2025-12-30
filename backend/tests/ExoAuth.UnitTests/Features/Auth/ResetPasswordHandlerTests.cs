using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.ResetPassword;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class ResetPasswordHandlerTests
{
    private readonly Mock<IPasswordResetService> _mockPasswordResetService;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IForceReauthService> _mockForceReauthService;
    private readonly Mock<ILogger<ResetPasswordHandler>> _mockLogger;
    private readonly ResetPasswordHandler _handler;

    public ResetPasswordHandlerTests()
    {
        _mockPasswordResetService = new Mock<IPasswordResetService>();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockForceReauthService = new Mock<IForceReauthService>();
        _mockLogger = new Mock<ILogger<ResetPasswordHandler>>();

        _handler = new ResetPasswordHandler(
            _mockPasswordResetService.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockForceReauthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ResetsPassword()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var resetToken = PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand("token123", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("newhash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPasswordHasher.Verify(x => x.Hash("NewPassword123!"), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockPasswordResetService.Verify(x => x.MarkAsUsedAsync(resetToken, It.IsAny<CancellationToken>()), Times.Once);
        _mockForceReauthService.Verify(x => x.SetFlagForAllSessionsAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordChangedAsync(
            user.Email,
            user.FirstName,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasswordResetCompleted,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCode_ResetsPassword()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var resetToken = PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand(null, "test@example.com", "ABCD-EFGH", "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("newhash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPasswordResetService.Verify(x => x.ValidateCodeAsync("test@example.com", "ABCD-EFGH", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var command = new ResetPasswordCommand("invalidtoken", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PasswordResetTokenInvalidException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ThrowsException()
    {
        // Arrange
        var command = new ResetPasswordCommand(null, "test@example.com", "XXXX-XXXX", "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PasswordResetTokenInvalidException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNoTokenOrCode_ThrowsException()
    {
        // Arrange
        var command = new ResetPasswordCommand(null, null, null, "NewPassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<PasswordResetTokenInvalidException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsException()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        user.Deactivate();
        var resetToken = PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand("token123", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<PasswordResetTokenInvalidException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ThrowsException()
    {
        // Arrange
        var resetToken = PasswordResetToken.Create(Guid.NewGuid(), "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand("token123", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PasswordResetTokenInvalidException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ForcesReauthOnAllSessions()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var resetToken = PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand("token123", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("newhash");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Force re-auth is called to invalidate all existing sessions (session-based)
        _mockForceReauthService.Verify(x => x.SetFlagForAllSessionsAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectMessage()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com");
        var resetToken = PasswordResetToken.Create(user.Id, "token123", "ABCD-EFGH");
        var command = new ResetPasswordCommand("token123", null, null, "NewPassword123!");

        _mockPasswordResetService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("newhash");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Message.Should().Contain("Password has been reset successfully");
    }
}
