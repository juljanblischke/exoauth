using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.ActivateSystemUser;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class ActivateSystemUserHandlerTests
{
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly ActivateSystemUserHandler _handler;

    public ActivateSystemUserHandlerTests()
    {
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new ActivateSystemUserHandler(
            _mockUserRepository.Object,
            _mockPermissionCache.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithDeactivatedUser_ActivatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new ActivateSystemUserCommand(userId);

        var user = CreateDeactivatedUser(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeTrue();

        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserActivated,
            currentUserId,
            userId,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateSystemUserCommand(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymized_ThrowsUserAnonymizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateSystemUserCommand(userId);

        var user = CreateAnonymizedUser(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserAnonymizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_ThrowsUserAlreadyActivatedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateSystemUserCommand(userId);

        var user = CreateActiveUser(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UserAlreadyActivatedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidatesPermissionCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var command = new ActivateSystemUserCommand(userId);

        var user = CreateDeactivatedUser(userId);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SystemUser CreateActiveUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static SystemUser CreateDeactivatedUser(Guid userId)
    {
        var user = CreateActiveUser(userId);
        user.Deactivate();
        return user;
    }

    private static SystemUser CreateAnonymizedUser(Guid userId)
    {
        var user = CreateActiveUser(userId);
        user.Anonymize();
        return user;
    }

    private static void SetUserId(SystemUser user, Guid userId)
    {
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);
    }
}
