using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Commands.UpdateInvite;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemInvites;

public sealed class UpdateInviteHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly UpdateInviteHandler _handler;
    private readonly DateTime _now = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public UpdateInviteHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUser.Setup(x => x.Email).Returns("admin@example.com");

        _handler = new UpdateInviteHandler(
            _mockContext.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidPendingInvite_UpdatesFirstName()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "OldFirst", "OldLast");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, "NewFirst", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FirstName.Should().Be("NewFirst");
        result.LastName.Should().Be("OldLast");
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidPendingInvite_UpdatesLastName()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "OldFirst", "OldLast");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, null, "NewLast", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FirstName.Should().Be("OldFirst");
        result.LastName.Should().Be("NewLast");
    }

    [Fact]
    public async Task Handle_WithValidPendingInvite_UpdatesPermissions()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var permission1 = TestDataFactory.CreateSystemPermission("system:users:read");
        var permission2 = TestDataFactory.CreateSystemPermission("system:users:update");
        SetEntityId(permission1, Guid.NewGuid());
        SetEntityId(permission2, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "First", "Last");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        var permissions = new List<SystemPermission> { permission1, permission2 };

        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);
        _mockContext.Setup(x => x.SystemPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(permissions).Object);

        var newPermissionIds = new List<Guid> { permission1.Id, permission2.Id };
        var command = new UpdateInviteCommand(inviteId, null, null, newPermissionIds);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        invite.GetPermissionIds().Should().BeEquivalentTo(newPermissionIds);
    }

    [Fact]
    public async Task Handle_WithMultipleFields_UpdatesAllFields()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var permission = TestDataFactory.CreateSystemPermission("system:users:read");
        SetEntityId(permission, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "OldFirst", "OldLast");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        var permissions = new List<SystemPermission> { permission };

        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);
        _mockContext.Setup(x => x.SystemPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(permissions).Object);

        var newPermissionIds = new List<Guid> { permission.Id };
        var command = new UpdateInviteCommand(inviteId, "NewFirst", "NewLast", newPermissionIds);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FirstName.Should().Be("NewFirst");
        result.LastName.Should().Be("NewLast");
        invite.GetPermissionIds().Should().BeEquivalentTo(newPermissionIds);
    }

    [Fact]
    public async Task Handle_WithNonExistentInvite_ThrowsNotFoundException()
    {
        // Arrange
        var invites = new List<SystemInvite>();
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(Guid.NewGuid(), "NewFirst", null, null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithAcceptedInvite_ThrowsInviteNotEditableException()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "First", "Last");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);
        invite.Accept(); // Mark as accepted

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, "NewFirst", null, null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InviteNotEditableException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_NOT_EDITABLE");
        exception.InviteId.Should().Be(inviteId);
    }

    [Fact]
    public async Task Handle_WithRevokedInvite_ThrowsInviteNotEditableException()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "First", "Last");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);
        invite.Revoke(); // Mark as revoked

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, "NewFirst", null, null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InviteNotEditableException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_NOT_EDITABLE");
    }

    [Fact]
    public async Task Handle_WithExpiredInvite_ThrowsInviteNotEditableException()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreateExpiredInvite("test@example.com", inviter, "First", "Last");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, "NewFirst", null, null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InviteNotEditableException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_NOT_EDITABLE");
    }

    [Fact]
    public async Task Handle_WithInvalidPermissionId_ThrowsSystemPermissionNotFoundException()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "First", "Last");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        var permissions = new List<SystemPermission>(); // Empty - no valid permissions

        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);
        _mockContext.Setup(x => x.SystemPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(permissions).Object);

        var invalidPermissionId = Guid.NewGuid();
        var command = new UpdateInviteCommand(inviteId, null, null, new List<Guid> { invalidPermissionId });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SystemPermissionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.PermissionId.Should().Be(invalidPermissionId);
    }

    [Fact]
    public async Task Handle_WithValidUpdate_LogsAudit()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreatePendingInvite("test@example.com", inviter, "OldFirst", "OldLast");
        var inviteId = Guid.NewGuid();
        SetEntityId(invite, inviteId);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new UpdateInviteCommand(inviteId, "NewFirst", null, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.InviteUpdated,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "SystemInvite",
            inviteId,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Helper methods
    private SystemInvite CreatePendingInvite(
        string email,
        SystemUser inviter,
        string firstName = "Test",
        string lastName = "User")
    {
        var invite = TestDataFactory.CreateSystemInvite(
            email,
            firstName,
            lastName,
            invitedBy: inviter.Id);

        SetInvitedByUser(invite, inviter);
        // Set expiration to future to make it pending
        SetExpiresAt(invite, DateTime.UtcNow.AddDays(1));

        return invite;
    }

    private SystemInvite CreateExpiredInvite(
        string email,
        SystemUser inviter,
        string firstName = "Test",
        string lastName = "User")
    {
        var invite = TestDataFactory.CreateSystemInvite(
            email,
            firstName,
            lastName,
            invitedBy: inviter.Id);

        SetInvitedByUser(invite, inviter);
        // Set expiration to past to make it expired
        SetExpiresAt(invite, DateTime.UtcNow.AddDays(-1));

        return invite;
    }

    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var idField = typeof(T).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
    }

    private static void SetInvitedByUser(SystemInvite invite, SystemUser inviter)
    {
        var property = typeof(SystemInvite)
            .GetProperty("InvitedByUser");
        property?.SetValue(invite, inviter);
    }

    private static void SetExpiresAt(SystemInvite invite, DateTime expiresAt)
    {
        var property = typeof(SystemInvite)
            .GetProperty("ExpiresAt");
        property?.SetValue(invite, expiresAt);
    }
}
