using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSession;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class RevokeUserSessionHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;

    public RevokeUserSessionHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailTemplateService.Setup(x => x.GetSubject(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string template, string lang) => $"Subject for {template}");
    }

    private RevokeUserSessionHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockRevokedSessionService.Object,
        _mockAuditService.Object,
        _mockEmailService.Object,
        _mockEmailTemplateService.Object);

    [Fact]
    public async Task Handle_WithValidSession_RevokesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var session = CreateSession(sessionId, userId);
        var sessions = new List<DeviceSession> { session };
        var refreshToken = RefreshToken.Create(userId, UserType.System, "token123", 30);
        SetRefreshTokenSessionId(refreshToken, sessionId);
        var refreshTokens = new List<RefreshToken> { refreshToken };

        SetupMockDbSets(users, sessions, refreshTokens);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Revoked.Should().BeTrue();

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        refreshToken.IsRevoked.Should().BeTrue();

        _mockAuditService.Verify(x => x.LogAsync(
            AuditActions.SessionRevokedByAdmin,
            adminUserId,
            userId,
            "DeviceSession",
            sessionId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "session-revoked-admin",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var users = new List<SystemUser>();

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsUserSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var sessions = new List<DeviceSession>(); // No sessions

        SetupMockDbSets(users, sessions, new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserSessionNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.SessionId.Should().Be(sessionId);
        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToOtherUser_ThrowsUserSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        // Session belongs to a different user
        var session = CreateSession(sessionId, otherUserId);
        var sessions = new List<DeviceSession> { session };

        SetupMockDbSets(users, sessions, new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserSessionNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.SessionId.Should().Be(sessionId);
        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithAnonymizedUser_DoesNotSendEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        user.Anonymize(); // Anonymize the user
        var users = new List<SystemUser> { user };
        var session = CreateSession(sessionId, userId);
        var sessions = new List<DeviceSession> { session };

        SetupMockDbSets(users, sessions, new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Revoked.Should().BeTrue();

        _mockEmailService.Verify(x => x.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupMockDbSets(
        List<SystemUser> users,
        List<DeviceSession> sessions,
        List<RefreshToken> refreshTokens)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockSessionsDbSet = CreateAsyncMockDbSet(sessions);
        var mockRefreshTokensDbSet = CreateAsyncMockDbSet(refreshTokens);

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockSessionsDbSet.Object);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokensDbSet.Object);
    }

    private static SystemUser CreateUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static DeviceSession CreateSession(Guid sessionId, Guid userId)
    {
        var session = DeviceSession.Create(userId, "device123", null, null, "Mozilla/5.0", "127.0.0.1");
        var idField = typeof(DeviceSession).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(session, sessionId);
        return session;
    }

    private static void SetUserId(SystemUser user, Guid userId)
    {
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);
    }

    private static void SetRefreshTokenSessionId(RefreshToken token, Guid sessionId)
    {
        var sessionIdField = typeof(RefreshToken)
            .GetProperty("DeviceSessionId")?
            .GetSetMethod(true);
        sessionIdField?.Invoke(token, new object[] { sessionId });
    }

    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsAsyncQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(queryable.Provider);

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

        mockSet.Setup(m => m.Remove(It.IsAny<T>()))
            .Callback<T>(entity => data.Remove(entity));

        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>()))
            .Callback<IEnumerable<T>>(entities =>
            {
                foreach (var entity in entities.ToList())
                {
                    data.Remove(entity);
                }
            });

        return mockSet;
    }
}
