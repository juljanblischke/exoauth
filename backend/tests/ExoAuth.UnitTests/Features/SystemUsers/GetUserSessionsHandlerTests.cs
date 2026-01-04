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
    private readonly Mock<IDeviceService> _mockDeviceService;

    public GetUserSessionsHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockDeviceService = new Mock<IDeviceService>();
    }

    private GetUserSessionsHandler CreateHandler() => new(
        _mockContext.Object,
        _mockDeviceService.Object);

    [Fact]
    public async Task Handle_WithValidUser_ReturnsDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        var devices = new List<Device> { device };

        SetupMockUserDbSet(users);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(deviceId);
        result[0].IsCurrent.Should().BeFalse(); // Admin view always returns false
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var users = new List<SystemUser>();
        SetupMockUserDbSet(users);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(query, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithNoDevices_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockUserDbSet(users);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsDevicesOrderedByLastUsed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserSessionsQuery(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var olderDevice = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var newerDevice = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        newerDevice.RecordUsage("192.168.1.1");

        var devices = new List<Device> { olderDevice, newerDevice };

        SetupMockUserDbSet(users);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].LastUsedAt.Should().BeOnOrAfter(result[1].LastUsedAt);
    }

    private void SetupMockUserDbSet(List<SystemUser> users)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
    }

    private static SystemUser CreateUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
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

        return mockSet;
    }
}
