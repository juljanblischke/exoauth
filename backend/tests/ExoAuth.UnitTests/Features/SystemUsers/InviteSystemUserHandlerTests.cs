using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.InviteSystemUser;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class InviteSystemUserHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly InviteSystemUserHandler _handler;

    public InviteSystemUserHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new InviteSystemUserHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesInviteAndSendsEmail()
    {
        // Arrange
        var inviterId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new InviteSystemUserCommand(
            Email: "newuser@example.com",
            FirstName: "New",
            LastName: "User",
            PermissionIds: new List<Guid> { permissionId }
        );

        var inviter = TestDataFactory.CreateSystemUser(email: "admin@example.com");
        var permission = TestDataFactory.CreateSystemPermission("system:users:read");

        // Setup invites DbSet (empty - no existing invites)
        var invites = new List<SystemInvite>();
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);

        // Setup permissions DbSet
        SetupPermissionId(permission, permissionId);
        var permissions = new List<SystemPermission> { permission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCurrentUser.Setup(x => x.UserId).Returns(inviterId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(inviterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@example.com");
        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("User");

        // Verify invite was saved
        _mockContext.Verify(x => x.SystemInvites.AddAsync(
            It.Is<SystemInvite>(i => i.Email == "newuser@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify email was sent
        _mockEmailService.Verify(x => x.SendSystemInviteAsync(
            "newuser@example.com",
            "New",
            inviter.FullName,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserInvited,
            inviterId,
            "SystemInvite",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsEmailExistsException()
    {
        // Arrange
        var command = new InviteSystemUserCommand(
            Email: "existing@example.com",
            FirstName: "Existing",
            LastName: "User",
            PermissionIds: new List<Guid> { Guid.NewGuid() }
        );

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<EmailExistsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Verify no invite was created
        _mockContext.Verify(x => x.SystemInvites.AddAsync(
            It.IsAny<SystemInvite>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyPending_ThrowsAuthException()
    {
        // Arrange
        var command = new InviteSystemUserCommand(
            Email: "pending@example.com",
            FirstName: "Pending",
            LastName: "User",
            PermissionIds: new List<Guid> { Guid.NewGuid() }
        );

        var existingInvite = TestDataFactory.CreateSystemInvite(email: "pending@example.com");
        var invites = new List<SystemInvite> { existingInvite };
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("AUTH_INVITE_PENDING");
    }

    [Fact]
    public async Task Handle_WhenPermissionNotFound_ThrowsSystemPermissionNotFoundException()
    {
        // Arrange
        var invalidPermissionId = Guid.NewGuid();
        var command = new InviteSystemUserCommand(
            Email: "newuser@example.com",
            FirstName: "New",
            LastName: "User",
            PermissionIds: new List<Guid> { invalidPermissionId }
        );

        // Empty invites DbSet
        var invites = new List<SystemInvite>();
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);

        // Empty permissions DbSet (no matching permissions)
        var permissions = new List<SystemPermission>();
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemPermissionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionId.Should().Be(invalidPermissionId);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsAuthException()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var command = new InviteSystemUserCommand(
            Email: "newuser@example.com",
            FirstName: "New",
            LastName: "User",
            PermissionIds: new List<Guid> { permissionId }
        );

        var permission = TestDataFactory.CreateSystemPermission();
        SetupPermissionId(permission, permissionId);

        // Empty invites
        var invites = new List<SystemInvite>();
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);

        // Permissions
        var permissions = new List<SystemPermission> { permission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null); // Not authenticated

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("AUTH_UNAUTHORIZED");
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var inviterId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var command = new InviteSystemUserCommand(
            Email: "NEWUSER@EXAMPLE.COM",
            FirstName: "New",
            LastName: "User",
            PermissionIds: new List<Guid> { permissionId }
        );

        var inviter = TestDataFactory.CreateSystemUser(email: "admin@example.com");
        var permission = TestDataFactory.CreateSystemPermission();
        SetupPermissionId(permission, permissionId);

        var invites = new List<SystemInvite>();
        var mockInvitesDbSet = CreateAsyncMockDbSet(invites);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockInvitesDbSet.Object);

        var permissions = new List<SystemPermission> { permission };
        var mockPermissionsDbSet = CreateAsyncMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(mockPermissionsDbSet.Object);

        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockCurrentUser.Setup(x => x.UserId).Returns(inviterId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(inviterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Email.Should().Be("newuser@example.com"); // lowercase

        // Verify email was sent to lowercase address
        _mockEmailService.Verify(x => x.SendSystemInviteAsync(
            "newuser@example.com",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper to create async mock DbSet using shared helper
    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        return MockDbContext.CreateAsyncMockDbSet(data);
    }

    // Helper to set the Id via reflection
    private static void SetupPermissionId(SystemPermission permission, Guid id)
    {
        var backingField = typeof(SystemPermission).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(permission, id);
    }
}

