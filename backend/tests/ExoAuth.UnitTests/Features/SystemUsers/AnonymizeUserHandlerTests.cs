using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.AnonymizeUser;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class AnonymizeUserHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;

    public AnonymizeUserHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockUserRepository = new Mock<ISystemUserRepository>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Default: user has no critical permissions
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
    }

    private AnonymizeUserHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockRevokedSessionService.Object,
        _mockAuditService.Object,
        _mockUserRepository.Object);

    [Fact]
    public async Task Handle_WithValidUser_AnonymizesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };
        var sessions = new List<DeviceSession>();
        var refreshTokens = new List<RefreshToken>();
        var backupCodes = new List<MfaBackupCode>();

        SetupMockDbSets(users, sessions, refreshTokens, backupCodes);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(userId);
        user.IsAnonymized.Should().BeTrue();

        _mockAuditService.Verify(x => x.LogAsync(
            AuditActions.UserAnonymized,
            adminUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenAnonymizingSelf_ThrowsCannotDeleteSelfException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<CannotDeleteSelfException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var users = new List<SystemUser>();

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyAnonymized_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        user.Anonymize(); // Already anonymized
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        // Should not call SaveChanges or Audit when already anonymized
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
    public async Task Handle_RevokesSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };
        var session = CreateDeviceSession(sessionId, userId);
        var sessions = new List<DeviceSession> { session };
        var refreshTokens = new List<RefreshToken>();
        var backupCodes = new List<MfaBackupCode>();

        SetupMockDbSets(users, sessions, refreshTokens, backupCodes);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RevokesRefreshTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };
        var sessions = new List<DeviceSession>();
        var refreshToken = RefreshToken.Create(userId, UserType.System, "token123", 30);
        var refreshTokens = new List<RefreshToken> { refreshToken };
        var backupCodes = new List<MfaBackupCode>();

        SetupMockDbSets(users, sessions, refreshTokens, backupCodes);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenLastPermissionHolder_ThrowsLastPermissionHolderException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        // User has system:users:update permission and is the only holder
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "system:users:update" });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync("system:users:update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<LastPermissionHolderException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenNotLastPermissionHolder_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        // User has system:users:update permission but there are 2 holders
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "system:users:update" });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync("system:users:update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.IsAnonymized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenLastUsersReadHolder_ThrowsLastPermissionHolderException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        // User has system:users:read permission and is the only holder
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "system:users:read" });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync("system:users:read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LastPermissionHolderException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionName.Should().Be("system:users:read");
    }

    [Fact]
    public async Task Handle_WhenNotLastUsersReadHolder_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        // User has system:users:read permission but there are 2 holders
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "system:users:read" });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync("system:users:read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.IsAnonymized.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeletesInvitesWithUserEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId, userEmail);
        var users = new List<SystemUser> { user };

        // Create invites - one with matching email, one with different email
        var inviteWithMatchingEmail = SystemInvite.Create(
            userEmail, "Test", "User", new List<Guid>(), adminUserId, "hash1");
        var inviteWithDifferentEmail = SystemInvite.Create(
            "other@example.com", "Other", "User", new List<Guid>(), adminUserId, "hash2");
        var invites = new List<SystemInvite> { inviteWithMatchingEmail, inviteWithDifferentEmail };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>(), invites);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Only the invite with matching email should be removed
        invites.Should().HaveCount(1);
        invites.Should().Contain(inviteWithDifferentEmail);
        invites.Should().NotContain(inviteWithMatchingEmail);
    }

    [Fact]
    public async Task Handle_DeletesAllInvitesWithUserEmail_RegardlessOfStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId, userEmail);
        var users = new List<SystemUser> { user };

        // Create multiple invites with the same email (simulating different statuses)
        var invite1 = SystemInvite.Create(userEmail, "Test", "User", new List<Guid>(), adminUserId, "hash1");
        var invite2 = SystemInvite.Create(userEmail, "Test", "User", new List<Guid>(), adminUserId, "hash2");
        var invites = new List<SystemInvite> { invite1, invite2 };

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>(), invites);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - All invites with matching email should be removed
        invites.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoInvitesExist_SucceedsWithoutError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new AnonymizeUserCommand(userId);

        var user = CreateUserWithPermissions(userId);
        var users = new List<SystemUser> { user };
        var invites = new List<SystemInvite>();

        SetupMockDbSets(users, new List<DeviceSession>(), new List<RefreshToken>(), new List<MfaBackupCode>(), invites);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        user.IsAnonymized.Should().BeTrue();
    }

    private void SetupMockDbSets(
        List<SystemUser> users,
        List<DeviceSession> sessions,
        List<RefreshToken> refreshTokens,
        List<MfaBackupCode> backupCodes,
        List<SystemInvite>? invites = null)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockSessionsDbSet = CreateAsyncMockDbSet(sessions);
        var mockRefreshTokensDbSet = CreateAsyncMockDbSet(refreshTokens);
        var mockBackupCodesDbSet = CreateAsyncMockDbSet(backupCodes);
        var mockPermissionsDbSet = CreateAsyncMockDbSet(new List<SystemUserPermission>());
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites ?? new List<SystemInvite>());

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockSessionsDbSet.Object);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokensDbSet.Object);
        _mockContext.Setup(x => x.MfaBackupCodes).Returns(mockBackupCodesDbSet.Object);
        _mockContext.Setup(x => x.SystemUserPermissions).Returns(mockPermissionsDbSet.Object);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);
    }

    private static SystemUser CreateUserWithPermissions(Guid userId, string email = "test@example.com")
    {
        var user = SystemUser.Create(email, "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static DeviceSession CreateDeviceSession(Guid sessionId, Guid userId)
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
