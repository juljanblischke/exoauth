using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.ResendPasswordReset;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Resend;

public sealed class ResendPasswordResetHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordResetService> _mockPasswordResetService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly Mock<ILogger<ResendPasswordResetHandler>> _mockLogger;
    private readonly ResendPasswordResetHandler _handler;
    private readonly List<PasswordResetToken> _tokens;

    public ResendPasswordResetHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordResetService = new Mock<IPasswordResetService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockLogger = new Mock<ILogger<ResendPasswordResetHandler>>();
        _tokens = new List<PasswordResetToken>();

        _mockContext.Setup(x => x.PasswordResetTokens)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_tokens).Object);

        _handler = new ResendPasswordResetHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPasswordResetService.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockCaptchaService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenUserNotFound()
    {
        // Arrange - User not found should still return success (to prevent email enumeration)
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        var command = new ResendPasswordResetCommand(
            Email: "nonexistent@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If an account exists");
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenUserInactive()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("inactive@example.com", isActive: false);
        TestDataFactory.SetEntityId(user, Guid.NewGuid());

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ResendPasswordResetCommand(
            Email: "inactive@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoExistingToken()
    {
        // Arrange - User exists but no password reset token
        var user = TestDataFactory.CreateSystemUser("user@example.com");
        var userId = Guid.NewGuid();
        TestDataFactory.SetEntityId(user, userId);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // No tokens in the list

        var command = new ResendPasswordResetCommand(
            Email: "user@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsCooldownException_WhenRequestedTooSoon()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com");
        TestDataFactory.SetEntityId(user, userId);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Add a recent token (created 30 seconds ago, within 60 second cooldown)
        var token = TestDataFactory.CreatePasswordResetToken(userId);
        TestDataFactory.SetEntityId(token, Guid.NewGuid());
        TestDataFactory.SetCreatedAt(token, DateTime.UtcNow.AddSeconds(-30));
        _tokens.Add(token);

        _mockContext.Setup(x => x.PasswordResetTokens)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_tokens).Object);

        var command = new ResendPasswordResetCommand(
            Email: "user@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PasswordResetResendCooldownException>();
    }

    [Fact]
    public async Task Handle_ResendsEmail_WhenCooldownPassed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John", lastName: "Doe");
        TestDataFactory.SetEntityId(user, userId);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Add an older token (created 90 seconds ago, past 60 second cooldown)
        var existingToken = TestDataFactory.CreatePasswordResetToken(userId);
        TestDataFactory.SetEntityId(existingToken, Guid.NewGuid());
        TestDataFactory.SetCreatedAt(existingToken, DateTime.UtcNow.AddSeconds(-90));
        _tokens.Add(existingToken);

        _mockContext.Setup(x => x.PasswordResetTokens)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_tokens).Object);

        // Create a new token for the result
        var newToken = TestDataFactory.CreatePasswordResetToken(userId, "new-token", "ABCD-1234");
        TestDataFactory.SetEntityId(newToken, Guid.NewGuid());

        _mockPasswordResetService.Setup(x => x.CreateResetTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetResult(newToken, "new-token", "ABCD-1234"));

        var command = new ResendPasswordResetCommand(
            Email: "user@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPasswordResetService.Verify(x => x.InvalidateAllTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPasswordResetService.Verify(x => x.CreateResetTokenAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(
            "user@example.com", "John", "new-token", "ABCD-1234", userId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidatesCaptcha()
    {
        // Arrange
        var command = new ResendPasswordResetCommand(
            Email: "user@example.com",
            CaptchaToken: "test-captcha",
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCaptchaService.Verify(x => x.ValidateRequiredAsync(
            "test-captcha",
            "forgot_password_resend",
            "127.0.0.1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog_WhenEmailSent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com");
        TestDataFactory.SetEntityId(user, userId);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var existingToken = TestDataFactory.CreatePasswordResetToken(userId);
        TestDataFactory.SetEntityId(existingToken, Guid.NewGuid());
        TestDataFactory.SetCreatedAt(existingToken, DateTime.UtcNow.AddSeconds(-90));
        _tokens.Add(existingToken);

        _mockContext.Setup(x => x.PasswordResetTokens)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_tokens).Object);

        var newToken = TestDataFactory.CreatePasswordResetToken(userId, "token", "ABCD-1234");
        TestDataFactory.SetEntityId(newToken, Guid.NewGuid());

        _mockPasswordResetService.Setup(x => x.CreateResetTokenAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetResult(newToken, "token", "ABCD-1234"));

        var command = new ResendPasswordResetCommand(
            Email: "user@example.com",
            CaptchaToken: "valid-captcha",
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasswordResetResent,
            userId,
            It.IsAny<Guid?>(),
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
