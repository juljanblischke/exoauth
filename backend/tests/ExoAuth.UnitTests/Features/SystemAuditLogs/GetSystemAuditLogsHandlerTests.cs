using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemAuditLogs.Queries.GetSystemAuditLogs;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemAuditLogs;

public sealed class GetSystemAuditLogsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetSystemAuditLogsHandler _handler;
    private readonly DateTime _now = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public GetSystemAuditLogsHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _handler = new GetSystemAuditLogsHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_SearchInDetails_FindsMatchingLogs()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("admin@example.com");
        SetEntityId(user, Guid.NewGuid());

        var logWithDetails = CreateAuditLog(
            "UserAnonymized",
            user,
            details: new { OriginalEmail = "john.doe@example.com", OriginalName = "John Doe" });
        var logWithoutMatch = CreateAuditLog(
            "UserCreated",
            user,
            details: new { Email = "other@example.com" });
        var logNoDetails = CreateAuditLog("UserLoggedIn", user);

        var logs = new List<SystemAuditLog> { logWithDetails, logWithoutMatch, logNoDetails };
        SetupMockDbSet(logs, new List<SystemUser> { user });

        var query = new GetSystemAuditLogsQuery(Search: "john.doe@example.com");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Action.Should().Be("UserAnonymized");
        result.Items[0].Details.Should().Contain("john.doe@example.com");
    }

    [Fact]
    public async Task Handle_SearchInDetails_IsCaseInsensitive()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("admin@example.com");
        SetEntityId(user, Guid.NewGuid());

        var logWithDetails = CreateAuditLog(
            "UserAnonymized",
            user,
            details: new { OriginalEmail = "John.Doe@Example.COM" });

        var logs = new List<SystemAuditLog> { logWithDetails };
        SetupMockDbSet(logs, new List<SystemUser> { user });

        // Search with lowercase
        var query = new GetSystemAuditLogsQuery(Search: "john.doe@example.com");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Action.Should().Be("UserAnonymized");
    }

    [Fact]
    public async Task Handle_SearchInUserEmail_StillWorks()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("admin@example.com", firstName: "Admin", lastName: "User");
        SetEntityId(user, Guid.NewGuid());

        var log = CreateAuditLog("UserLoggedIn", user);

        var logs = new List<SystemAuditLog> { log };
        SetupMockDbSet(logs, new List<SystemUser> { user });

        var query = new GetSystemAuditLogsQuery(Search: "admin@example.com");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_SearchMatchesBothUserAndDetails()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("admin@example.com");
        SetEntityId(user, Guid.NewGuid());

        var logMatchesUser = CreateAuditLog("UserLoggedIn", user);
        var logMatchesDetails = CreateAuditLog(
            "UserAnonymized",
            null,
            details: new { OriginalEmail = "admin@example.com" });

        var logs = new List<SystemAuditLog> { logMatchesUser, logMatchesDetails };
        SetupMockDbSet(logs, new List<SystemUser> { user });

        var query = new GetSystemAuditLogsQuery(Search: "admin@example.com");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoSearch_ReturnsAllLogs()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser("admin@example.com");
        SetEntityId(user, Guid.NewGuid());

        var log1 = CreateAuditLog("UserLoggedIn", user);
        var log2 = CreateAuditLog("UserCreated", user, details: new { Email = "new@example.com" });
        var log3 = CreateAuditLog("UserAnonymized", user);

        var logs = new List<SystemAuditLog> { log1, log2, log3 };
        SetupMockDbSet(logs, new List<SystemUser> { user });

        var query = new GetSystemAuditLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
    }

    // Helper methods
    private SystemAuditLog CreateAuditLog(
        string action,
        SystemUser? user,
        SystemUser? targetUser = null,
        object? details = null)
    {
        var log = SystemAuditLog.Create(
            action,
            userId: user?.Id,
            targetUserId: targetUser?.Id,
            details: details
        );

        SetEntityId(log, Guid.NewGuid());
        SetCreatedAt(log, _now);

        if (user != null)
        {
            SetUser(log, user);
        }

        if (targetUser != null)
        {
            SetTargetUser(log, targetUser);
        }

        return log;
    }

    private void SetupMockDbSet(List<SystemAuditLog> logs, List<SystemUser> users)
    {
        _mockContext.Setup(x => x.SystemAuditLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(logs).Object);
        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(users).Object);
    }

    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var idField = typeof(T).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
    }

    private static void SetCreatedAt<T>(T entity, DateTime createdAt) where T : class
    {
        var property = typeof(T).BaseType?
            .GetProperty("CreatedAt");
        property?.SetValue(entity, createdAt);
    }

    private static void SetUser(SystemAuditLog log, SystemUser user)
    {
        var property = typeof(SystemAuditLog)
            .GetProperty("User");
        property?.SetValue(log, user);
    }

    private static void SetTargetUser(SystemAuditLog log, SystemUser user)
    {
        var property = typeof(SystemAuditLog)
            .GetProperty("TargetUser");
        property?.SetValue(log, user);
    }
}
