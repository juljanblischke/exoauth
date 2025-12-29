using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.DeactivateSystemUser;
using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using static ExoAuth.UnitTests.Helpers.MockDbContext;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class DeactivateSystemUserHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly DeactivateSystemUserHandler _handler;

    public DeactivateSystemUserHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Setup empty DbSets
        _mockContext.Setup(x => x.DeviceSessions).Returns(CreateMockDbSet(new List<DeviceSession>()).Object);
        _mockContext.Setup(x => x.RefreshTokens).Returns(CreateMockDbSet(new List<RefreshToken>()).Object);

        _handler = new DeactivateSystemUserHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPermissionCache.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object,
            _mockRevokedSessionService.Object);
    }

    [Fact]
    public async Task Handle_WithValidUser_DeactivatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        var user = CreateUser(userId, isActive: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeFalse();

        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserDeactivated,
            currentUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeactivatingSelf_ThrowsCannotDeleteSelfException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        await Assert.ThrowsAsync<CannotDeleteSelfException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyDeactivated_ThrowsUserAlreadyDeactivatedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        var user = CreateUser(userId, isActive: false);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserAlreadyDeactivatedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenLastUsersUpdateHolder_ThrowsLastPermissionHolderException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        var user = CreateUser(userId, isActive: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { SystemPermissions.UsersUpdate });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync(SystemPermissions.UsersUpdate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LastPermissionHolderException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionName.Should().Be(SystemPermissions.UsersUpdate);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOthersHaveUsersUpdate_AllowsDeactivation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        var user = CreateUser(userId, isActive: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { SystemPermissions.UsersUpdate });
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync(SystemPermissions.UsersUpdate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // Multiple users have this permission

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidatesPermissionCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new DeactivateSystemUserCommand(userId);

        var user = CreateUser(userId, isActive: true);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetUserPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SystemUser CreateUser(Guid userId, bool isActive = true)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);

        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }
}
