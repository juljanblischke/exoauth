using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RefreshToken;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class RefreshTokenHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklist;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<IDeviceSessionService> _mockDeviceSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenBlacklist = new Mock<ITokenBlacklistService>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockDeviceSessionService = new Mock<IDeviceSessionService>();
        _mockAuditService = new Mock<IAuditService>();

        // Default token service setup
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(30));

        _handler = new RefreshTokenHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockTokenBlacklist.Object,
            _mockPermissionCache.Object,
            _mockDeviceSessionService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "valid-refresh-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        var tokens = new List<RefreshToken> { storedToken };
        var permissions = new List<string> { "system:users:read" };

        // We need to setup the user with the same ID as the token
        SetupUserIdOnToken(storedToken, userId);

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid?>()))
            .Returns("new-access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");

        // Verify old token was blacklisted
        _mockTokenBlacklist.Verify(x => x.BlacklistAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify new token was saved
        _mockContext.Verify(x => x.RefreshTokens.AddAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.TokenRefreshed,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "SystemUser",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-token");

        var tokens = new List<RefreshToken>(); // No tokens
        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "revoked-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        storedToken.Revoke(); // Token is revoked

        var tokens = new List<RefreshToken> { storedToken };
        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithBlacklistedToken_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "blacklisted-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        var tokens = new List<RefreshToken> { storedToken };

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Token is blacklisted

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNonSystemUserType_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "org-user-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        // Create a token with Organization user type (not System)
        var storedToken = RefreshToken.Create(userId, UserType.Organization, refreshTokenString, 30);
        var tokens = new List<RefreshToken> { storedToken };

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "valid-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        user.Deactivate(); // User is inactive

        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        SetupUserIdOnToken(storedToken, userId);
        var tokens = new List<RefreshToken> { storedToken };

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ThrowsInvalidRefreshTokenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "valid-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        SetupUserIdOnToken(storedToken, userId);
        var tokens = new List<RefreshToken> { storedToken };

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null); // User not found

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_RevokesOldTokenBeforeCreatingNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenString = "valid-token";
        var command = new RefreshTokenCommand(refreshTokenString);

        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var storedToken = RefreshToken.Create(userId, UserType.System, refreshTokenString, 30);
        SetupUserIdOnToken(storedToken, userId);
        var tokens = new List<RefreshToken> { storedToken };
        var permissions = new List<string> { "system:users:read" };

        var mockRefreshTokenDbSet = CreateAsyncMockDbSet(tokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenDbSet.Object);

        _mockTokenBlacklist.Setup(x => x.IsBlacklistedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid?>()))
            .Returns("new-access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - old token should be revoked
        storedToken.IsRevoked.Should().BeTrue();
    }

    // Helper to create async mock DbSet using shared helper
    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        return MockDbContext.CreateAsyncMockDbSet(data);
    }

    // Helper to set the UserId via reflection since it's a private setter
    private static void SetupUserIdOnToken(RefreshToken token, Guid userId)
    {
        var backingField = typeof(RefreshToken).GetField("<UserId>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(token, userId);
    }
}
