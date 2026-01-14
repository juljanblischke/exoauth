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
    private readonly Mock<IDeviceService> _mockDeviceService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;

    public RevokeUserSessionHandlerTests()
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

        _mockDeviceService.Setup(x => x.RevokeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private RevokeUserSessionHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockDeviceService.Object,
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
        var device = TestDataFactory.CreateDeviceWithId(sessionId, userId);

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Revoked.Should().BeTrue();

        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockDeviceService.Verify(x => x.RevokeAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.SessionRevokedByAdmin,
            adminUserId,
            userId,
            "Device",
            sessionId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "session-revoked-admin",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            user.Id,
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
        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenDeviceNotFound_ThrowsUserSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserSessionNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.SessionId.Should().Be(sessionId);
        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenDeviceBelongsToOtherUser_ThrowsUserSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new RevokeUserSessionCommand(userId, sessionId);

        var user = CreateUser(userId);
        var users = new List<SystemUser> { user };

        // Device belongs to a different user
        var device = TestDataFactory.CreateDeviceWithId(sessionId, otherUserId);

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

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
        var device = TestDataFactory.CreateDeviceWithId(sessionId, userId);

        SetupMockUserDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);
        _mockDeviceService.Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

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
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
