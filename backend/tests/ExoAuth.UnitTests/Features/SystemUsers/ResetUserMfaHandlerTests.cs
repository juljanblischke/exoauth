using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.ResetUserMfa;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class ResetUserMfaHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IForceReauthService> _mockForceReauthService;
    private readonly Mock<ITokenBlacklistService> _mockTokenBlacklistService;

    public ResetUserMfaHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockForceReauthService = new Mock<IForceReauthService>();
        _mockTokenBlacklistService = new Mock<ITokenBlacklistService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEmailTemplateService.Setup(x => x.GetSubject(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string template, string lang) => $"Subject for {template}");
    }

    private ResetUserMfaHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockAuditService.Object,
        _mockEmailService.Object,
        _mockEmailTemplateService.Object,
        _mockForceReauthService.Object,
        _mockTokenBlacklistService.Object);

    [Fact]
    public async Task Handle_WithMfaEnabledUser_ResetsMfa()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new ResetUserMfaCommand(userId, "User lost device");

        var user = CreateUserWithMfa(userId);
        var users = new List<SystemUser> { user };
        var backupCodes = new List<MfaBackupCode>();

        SetupMockDbSets(users, backupCodes);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.MfaResetByAdmin,
            adminUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "mfa-reset-admin",
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
        var command = new ResetUserMfaCommand(userId);

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
        var command = new ResetUserMfaCommand(userId);

        var users = new List<SystemUser>();

        SetupMockDbSets(users, new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenMfaNotEnabled_ThrowsMfaNotEnabledException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new ResetUserMfaCommand(userId);

        var user = CreateUserWithoutMfa(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<MfaNotEnabledException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletesBackupCodes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var command = new ResetUserMfaCommand(userId);

        var user = CreateUserWithMfa(userId);
        var users = new List<SystemUser> { user };
        var backupCode = CreateBackupCode(userId);
        var backupCodes = new List<MfaBackupCode> { backupCode };

        SetupMockDbSets(users, backupCodes);
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - backup codes should be removed
        backupCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SendsEmailNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var reason = "User requested MFA reset";
        var command = new ResetUserMfaCommand(userId, reason);

        var user = CreateUserWithMfa(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<MfaBackupCode>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(adminUserId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            "mfa-reset-admin",
            It.Is<Dictionary<string, string>>(d =>
                d["firstName"] == user.FirstName &&
                d["reason"] == reason),
            user.PreferredLanguage,
            user.Id,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupMockDbSets(List<SystemUser> users, List<MfaBackupCode> backupCodes)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockBackupCodesDbSet = CreateAsyncMockDbSet(backupCodes);
        var mockRefreshTokensDbSet = CreateAsyncMockDbSet(new List<RefreshToken>());

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.MfaBackupCodes).Returns(mockBackupCodesDbSet.Object);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokensDbSet.Object);
    }

    private static SystemUser CreateUserWithMfa(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.SetMfaSecret("encrypted_secret");
        user.EnableMfa();
        return user;
    }

    private static SystemUser CreateUserWithoutMfa(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static MfaBackupCode CreateBackupCode(Guid userId)
    {
        return MfaBackupCode.Create(userId, "BACKUP1234");
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
