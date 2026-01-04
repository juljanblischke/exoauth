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
    private readonly Mock<IDeviceService> _mockDeviceService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;

    public RevokeUserSessionsHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockDeviceService = new Mock<IDeviceService>();
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
        _mockDeviceService.Object,
        _mockRevokedSessionService.Object,
        _mockAuditService.Object,
        _mockEmailService.Object,
        _mockEmailTemplateService.Object);

    [Fact]
    public async Task Handle_WithDevices_RevokesAllDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        var devices = new List<Device> { device };

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockDeviceService.Setup(x => x.RemoveAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RevokedCount.Should().Be(1);

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
        _mockDeviceService.Verify(x => x.RemoveAllAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

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
        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithNoDevices_ReturnsZeroCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RevokedCount.Should().Be(0);

        // Should not call audit or email when no devices to revoke
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
    public async Task Handle_RevokesMultipleDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var device1 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var device2 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var device3 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var devices = new List<Device> { device1, device2, device3 };

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockDeviceService.Setup(x => x.RemoveAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.RevokedCount.Should().Be(3);

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_SendsEmailWithCorrectDeviceCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new RevokeUserSessionsCommand(userId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        var device1 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var device2 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var devices = new List<Device> { device1, device2 };

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockDeviceService.Setup(x => x.RemoveAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

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
