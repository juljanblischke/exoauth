using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.UpdatePermissions;
using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class UpdatePermissionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly UpdateSystemUserPermissionsHandler _handler;

    public UpdatePermissionsHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new UpdateSystemUserPermissionsHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPermissionCache.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidPermissions_UpdatesUserPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { permissionId }
        );

        var user = CreateUserWithPermissions(userId, new List<string>());
        var permission = CreatePermissionWithId(permissionId, "system:users:read", "Test", "Test");

        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<SystemPermission> { permission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // More than 1 user has this permission

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);

        // After update, return user with new permissions
        var updatedUser = CreateUserWithPermissions(userId, new List<string> { "system:users:read" }, permissionId);
        _mockUserRepository.SetupSequence(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user)
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);

        // Verify permissions were updated
        _mockUserRepository.Verify(x => x.SetUserPermissionsAsync(
            userId,
            It.Is<List<Guid>>(ids => ids.Contains(permissionId)),
            currentUserId,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify cache was invalidated
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserPermissionsUpdated,
            currentUserId,
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
        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { Guid.NewGuid() }
        );

        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenPermissionNotFound_ThrowsSystemPermissionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidPermissionId = Guid.NewGuid();
        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { invalidPermissionId }
        );

        var user = CreateUserWithPermissions(userId, new List<string>());
        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Empty permissions - none found
        var permissions = new List<SystemPermission>();
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemPermissionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionId.Should().Be(invalidPermissionId);
    }

    [Fact]
    public async Task Handle_WhenRemovingLastUsersUpdatePermission_ThrowsLastPermissionHolderException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var usersUpdatePermissionId = Guid.NewGuid();
        var otherPermissionId = Guid.NewGuid();

        // User currently has system:users:update permission
        var user = CreateUserWithPermissions(userId, new List<string> { SystemPermissions.UsersUpdate }, usersUpdatePermissionId);

        // Command removes the permission by not including it
        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { otherPermissionId }
        );

        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Setup available permissions (includes the other permission)
        var otherPermission = CreatePermissionWithId(otherPermissionId, "system:users:read", "Read users", "Users");
        var permissions = new List<SystemPermission> { otherPermission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        // Only 1 user has this permission (the current user)
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync(SystemPermissions.UsersUpdate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LastPermissionHolderException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionName.Should().Be(SystemPermissions.UsersUpdate);

        // Verify permissions were NOT updated
        _mockUserRepository.Verify(x => x.SetUserPermissionsAsync(
            It.IsAny<Guid>(),
            It.IsAny<List<Guid>>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRemovingUsersUpdateButOthersHaveIt_AllowsRemoval()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var usersUpdatePermissionId = Guid.NewGuid();
        var otherPermissionId = Guid.NewGuid();

        // User currently has system:users:update permission
        var user = CreateUserWithPermissions(userId, new List<string> { SystemPermissions.UsersUpdate }, usersUpdatePermissionId);

        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { otherPermissionId }
        );

        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otherPermission = CreatePermissionWithId(otherPermissionId, "system:users:read", "Read users", "Users");
        var permissions = new List<SystemPermission> { otherPermission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        // Multiple users have this permission
        _mockUserRepository.Setup(x => x.CountUsersWithPermissionAsync(SystemPermissions.UsersUpdate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);

        var updatedUser = CreateUserWithPermissions(userId, new List<string> { "system:users:read" }, otherPermissionId);
        _mockUserRepository.SetupSequence(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user)
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify permissions were updated
        _mockUserRepository.Verify(x => x.SetUserPermissionsAsync(
            userId,
            It.Is<List<Guid>>(ids => ids.Contains(otherPermissionId)),
            currentUserId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidatesCacheAfterUpdate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new UpdateSystemUserPermissionsCommand(
            UserId: userId,
            PermissionIds: new List<Guid> { permissionId }
        );

        var user = CreateUserWithPermissions(userId, new List<string>());
        var permission = CreatePermissionWithId(permissionId, "system:users:read", "Test", "Test");

        _mockUserRepository.Setup(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var permissions = new List<SystemPermission> { permission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);

        var updatedUser = CreateUserWithPermissions(userId, new List<string> { "system:users:read" }, permissionId);
        _mockUserRepository.SetupSequence(x => x.GetByIdWithPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user)
            .ReturnsAsync(updatedUser);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - cache should be invalidated
        _mockPermissionCache.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper methods
    private static SystemUser CreateUserWithPermissions(Guid userId, List<string> permissionNames, Guid? permissionId = null)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);

        // Set the Id via reflection
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);

        // For testing, we need to set up permissions collection
        // This is a simplified approach - in real tests you might need a more sophisticated setup
        if (permissionNames.Any())
        {
            var permissionsField = typeof(SystemUser)
                .GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (permissionsField != null)
            {
                var permissionsList = new List<SystemUserPermission>();
                foreach (var name in permissionNames)
                {
                    var permission = SystemPermission.Create(name, "Description", "Category");
                    if (permissionId.HasValue)
                    {
                        var permIdField = typeof(SystemPermission).BaseType?
                            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        permIdField?.SetValue(permission, permissionId.Value);
                    }

                    var userPermission = CreateSystemUserPermission(user, permission);
                    permissionsList.Add(userPermission);
                }
                permissionsField.SetValue(user, permissionsList);
            }
        }

        return user;
    }

    private static SystemUserPermission CreateSystemUserPermission(SystemUser user, SystemPermission permission)
    {
        // Create via reflection since constructor is private
        var instance = System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(SystemUserPermission)) as SystemUserPermission;

        var userIdField = typeof(SystemUserPermission)
            .GetProperty("SystemUserId")?.GetBackingField();
        userIdField?.SetValue(instance, user.Id);

        var permissionField = typeof(SystemUserPermission)
            .GetProperty("SystemPermission");
        permissionField?.SetValue(instance, permission);

        return instance!;
    }

    private static SystemPermission CreatePermissionWithId(Guid id, string name, string description, string category)
    {
        var permission = SystemPermission.Create(name, description, category);
        var idField = typeof(SystemPermission).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(permission, id);
        return permission;
    }

    // Async DbSet helpers
    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        return mockSet;
    }
}

// Helper extension
internal static class PropertyInfoExtensions
{
    public static System.Reflection.FieldInfo? GetBackingField(this System.Reflection.PropertyInfo property)
    {
        return property.DeclaringType?.GetField($"<{property.Name}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }
}
