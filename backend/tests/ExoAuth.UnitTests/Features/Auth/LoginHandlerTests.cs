using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.Login;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class LoginHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IBruteForceProtectionService> _mockBruteForceService;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<IForceReauthService> _mockForceReauthService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockBruteForceService = new Mock<IBruteForceProtectionService>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockForceReauthService = new Mock<IForceReauthService>();
        _mockAuditService = new Mock<IAuditService>();

        // Default token service setup
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(30));

        _handler = new LoginHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            _mockBruteForceService.Object,
            _mockPermissionCache.Object,
            _mockForceReauthService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var permissions = new List<string> { "system:users:read", "system:users:create" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("test@example.com");
        result.User.Permissions.Should().BeEquivalentTo(permissions);

        _mockBruteForceService.Verify(x => x.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserLogin,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBlocked_ThrowsTooManyAttemptsException()
    {
        // Arrange
        var command = new LoginCommand("blocked@example.com", "Password123!");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<TooManyAttemptsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.LoginBlocked,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("notfound@example.com", "Password123!");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockBruteForceService.Verify(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockBruteForceService.Verify(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsUserInactiveException()
    {
        // Arrange
        var command = new LoginCommand("inactive@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "inactive@example.com", passwordHash: "hashed", isActive: true);
        user.Deactivate(); // Make user inactive

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<UserInactiveException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithFailedAttempts_RecordsFailedAttemptAndBlocksAfterMax()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, true)); // 5th attempt, now blocked

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Should log both failed attempt and blocked events
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserLoginFailed,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.LoginBlocked,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SavesRefreshTokenToDatabase()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var permissions = new List<string> { "system:users:read" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.RefreshTokens.AddAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var command = new LoginCommand("TEST@EXAMPLE.COM", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Verify email was normalized to lowercase
        _mockUserRepository.Verify(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }
}
