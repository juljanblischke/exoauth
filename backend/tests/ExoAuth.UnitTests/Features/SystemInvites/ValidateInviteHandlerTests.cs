using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.SystemInvites.Queries.ValidateInvite;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemInvites;

public sealed class ValidateInviteHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemInviteService> _mockInviteService;
    private readonly ValidateInviteHandler _handler;

    public ValidateInviteHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockInviteService = new Mock<ISystemInviteService>();
        _handler = new ValidateInviteHandler(_mockContext.Object, _mockInviteService.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsValidResponse()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com", firstName: "Admin", lastName: "User");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            permissionIds: new List<Guid> { permissionId },
            invitedBy: inviter.Id);
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);

        var token = "test-token";

        // Mock invite service to return the invite
        _mockInviteService.Setup(x => x.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var permission = TestDataFactory.CreateSystemPermission("system:users:read", "Read users", "Users");
        SetEntityId(permission, permissionId);
        var permissions = new List<SystemPermission> { permission };
        _mockContext.Setup(x => x.SystemPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(permissions).Object);

        var query = new ValidateInviteQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Valid.Should().BeTrue();
        result.Email.Should().Be("invited@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.InvitedBy.Should().NotBeNull();
        result.InvitedBy!.FullName.Should().Be("Admin User");
        result.Permissions.Should().HaveCount(1);
        result.Permissions![0].Name.Should().Be("system:users:read");
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsInvalidResponse()
    {
        // Arrange
        _mockInviteService.Setup(x => x.ValidateTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemInvite?)null);

        var query = new ValidateInviteQuery("invalid-token");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Valid.Should().BeFalse();
        result.Email.Should().BeNull();
        result.ErrorCode.Should().Be(ErrorCodes.AuthInviteInvalid);
        result.ErrorMessage.Should().Be("Invalid invitation token");
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyAccepted_ReturnsInvalidResponse()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, Guid.NewGuid());
        SetInvitedByUser(invite, inviter);
        invite.Accept();

        var token = "test-token";

        _mockInviteService.Setup(x => x.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var query = new ValidateInviteQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Valid.Should().BeFalse();
        result.Email.Should().Be("invited@example.com");
        result.ErrorCode.Should().Be("INVITE_ALREADY_ACCEPTED");
        result.ErrorMessage.Should().Contain("already been accepted");
    }

    [Fact]
    public async Task Handle_WhenInviteRevoked_ReturnsInvalidResponse()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, Guid.NewGuid());
        SetInvitedByUser(invite, inviter);
        invite.Revoke();

        var token = "test-token";

        _mockInviteService.Setup(x => x.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var query = new ValidateInviteQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Valid.Should().BeFalse();
        result.Email.Should().Be("invited@example.com");
        result.ErrorCode.Should().Be("INVITE_REVOKED");
        result.ErrorMessage.Should().Contain("revoked");
    }

    [Fact]
    public async Task Handle_WhenInviteExpired_ReturnsInvalidResponse()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, Guid.NewGuid());
        SetInvitedByUser(invite, inviter);

        // Set ExpiresAt to past
        SetExpiresAt(invite, DateTime.UtcNow.AddHours(-1));

        var token = "test-token";

        _mockInviteService.Setup(x => x.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var query = new ValidateInviteQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Valid.Should().BeFalse();
        result.Email.Should().Be("invited@example.com");
        result.ErrorCode.Should().Be(ErrorCodes.AuthInviteExpired);
        result.ErrorMessage.Should().Contain("expired");
    }

    // Helper methods
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
        var field = typeof(SystemInvite)
            .GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(invite, expiresAt);
    }
}
