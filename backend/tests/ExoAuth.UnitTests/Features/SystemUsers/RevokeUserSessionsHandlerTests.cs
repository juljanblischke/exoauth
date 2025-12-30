using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.RevokeUserSessions;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class RevokeUserSessionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;

    public RevokeUserSessionsHandlerTests()
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

    private RevokeUserSessionsHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockRevokedSessionService.Object,
        _mockAuditService.Object,
        _mockEmailService.Object,
        _mockEmailTemplateService.Object);

    [Fact]
    public async Task Handle_WithSessions_RevokesAllSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var session = CreateSession(sessionId, userId);
        var sessions = new List<DeviceSession> { session };
        var refreshToken = RefreshToken.Create(userId, UserType.System, "token123", 30);
        var refreshTokens = new List<RefreshToken> { refreshToken };

        SetupMockDbSets(users, sessions, refreshTokens);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RevokedCount.Should().Be(1);

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        refreshToken.IsRevoked.Should().BeTrue();

        _mockAuditService.Verify(x => x.LogAsync(
            AuditActions.SessionsRevokedByAdmin,
            adminUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "sessions-revoked-admin",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

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
        var command = new RevokeUserSessionsCommand(userId);

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
    public async Task Handle_WithNoSessions_ReturnsZeroCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RevokedCount.Should().Be(0);

        // Should not call audit or email when no sessions to revoke
        _mockAuditService.Verify(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockEmailService.Verify(x => x.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokesMultipleSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var session1 = CreateSession(Guid.NewGuid(), userId);
        var session2 = CreateSession(Guid.NewGuid(), userId);
        var session3 = CreateSession(Guid.NewGuid(), userId);
        var sessions = new List<DeviceSession> { session1, session2, session3 };

        SetupMockDbSets(users, sessions, new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.RevokedCount.Should().Be(3);

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_RevokesMultipleRefreshTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var session = CreateSession(Guid.NewGuid(), userId);
        var sessions = new List<DeviceSession> { session };

        var token1 = RefreshToken.Create(userId, UserType.System, "token1", 30);
        var token2 = RefreshToken.Create(userId, UserType.System, "token2", 30);
        var refreshTokens = new List<RefreshToken> { token1, token2 };

        SetupMockDbSets(users, sessions, refreshTokens);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        token1.IsRevoked.Should().BeTrue();
        token2.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SendsEmailWithCorrectSessionCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var session1 = CreateSession(Guid.NewGuid(), userId);
        var session2 = CreateSession(Guid.NewGuid(), userId);
        var sessions = new List<DeviceSession> { session1, session2 };

        SetupMockDbSets(users, sessions, new List<RefreshToken>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "sessions-revoked-admin",
            It.Is<Dictionary<string, string>>(d =>
                d["firstName"] == user.FirstName &&
                d["sessionCount"] == "2"),
            user.PreferredLanguage,
            It.IsAny<CancellationToken>()), Times.Once);
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
