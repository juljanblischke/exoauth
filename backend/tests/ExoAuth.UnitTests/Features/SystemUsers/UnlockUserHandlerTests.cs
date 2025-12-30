using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.UnlockUser;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class UnlockUserHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IBruteForceProtectionService> _mockBruteForceService;

    public UnlockUserHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockBruteForceService = new Mock<IBruteForceProtectionService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailTemplateService.Setup(x => x.GetSubject(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string template, string lang) => $"Subject for {template}");
    }

    private UnlockUserHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockAuditService.Object,
        _mockEmailService.Object,
        _mockEmailTemplateService.Object,
        _mockBruteForceService.Object);

    [Fact]
    public async Task Handle_WithLockedUser_UnlocksUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new UnlockUserCommand(userId, "User contacted support");

        var user = CreateLockedUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        user.IsLocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);

        _mockAuditService.Verify(x => x.LogAsync(
            AuditActions.AccountUnlockedByAdmin,
            adminUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "account-unlocked",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UnlockUserCommand(userId);

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
        var command = new UnlockUserCommand(userId);

        var users = new List<SystemUser>();

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenUserNotLocked_ReturnsSuccessWithoutAction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new UnlockUserCommand(userId);

        var user = CreateUnlockedUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        // Should not call SaveChanges or Audit when user is not locked
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockAuditService.Verify(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasFailedAttempts_ResetsFailedAttempts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new UnlockUserCommand(userId);

        var user = CreateUserWithFailedAttempts(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SendsEmailNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new UnlockUserCommand(userId, "Account unlocked by admin");

        var user = CreateLockedUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "account-unlocked",
            It.Is<Dictionary<string, string>>(d => d["firstName"] == user.FirstName),
            user.PreferredLanguage,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupMockDbSet(List<SystemUser> users)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
    }

    private static SystemUser CreateLockedUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.Lock(DateTime.UtcNow.AddHours(1));
        return user;
    }

    private static SystemUser CreateUnlockedUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static SystemUser CreateUserWithFailedAttempts(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        // Simulate failed attempts
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
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
