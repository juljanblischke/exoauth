using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Queries.GetUserSessions;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class GetUserSessionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;

    public GetUserSessionsHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
    }

    private GetUserSessionsHandler CreateHandler() => new(_mockContext.Object);

    [Fact]
    public async Task Handle_WithValidUser_ReturnsSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var session = CreateSession(sessionId, userId);
        var sessions = new List<DeviceSession> { session };

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(sessionId);
        result[0].IsCurrent.Should().BeFalse(); // Admin view always returns false
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var users = new List<SystemUser>();
        var sessions = new List<DeviceSession>();

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(query, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var sessions = new List<DeviceSession>();

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesRevokedSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var activeSession = CreateSession(Guid.NewGuid(), userId);
        var revokedSession = CreateSession(Guid.NewGuid(), userId);
        revokedSession.Revoke();

        var sessions = new List<DeviceSession> { activeSession, revokedSession };

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(activeSession.Id);
    }

    [Fact]
    public async Task Handle_ReturnsSessionsOrderedByLastActivity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var olderSession = CreateSession(Guid.NewGuid(), userId);
        var newerSession = CreateSession(Guid.NewGuid(), userId);
        newerSession.RecordActivity(); // This updates LastActivityAt

        var sessions = new List<DeviceSession> { olderSession, newerSession };

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].LastActivityAt.Should().BeOnOrAfter(result[1].LastActivityAt);
    }

    [Fact]
    public async Task Handle_OnlyReturnsSessionsForSpecifiedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var otherUser = CreateUser(otherUserId);
        var users = new List<SystemUser> { user, otherUser };

        var userSession = CreateSession(Guid.NewGuid(), userId);
        var otherUserSession = CreateSession(Guid.NewGuid(), otherUserId);

        var sessions = new List<DeviceSession> { userSession, otherUserSession };

        SetupMockDbSets(users, sessions);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.All(s => s.Id == userSession.Id).Should().BeTrue();
    }

    private void SetupMockDbSets(List<SystemUser> users, List<DeviceSession> sessions)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockSessionsDbSet = CreateAsyncMockDbSet(sessions);

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockSessionsDbSet.Object);
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
        SetSessionId(session, sessionId);
        session.SetDeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        return session;
    }

    private static void SetUserId(SystemUser user, Guid userId)
    {
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);
    }

    private static void SetSessionId(DeviceSession session, Guid sessionId)
    {
        var idField = typeof(DeviceSession).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(session, sessionId);
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

        return mockSet;
    }
}
