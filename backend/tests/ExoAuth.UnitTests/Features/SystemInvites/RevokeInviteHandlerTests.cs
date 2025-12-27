using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Commands.RevokeInvite;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemInvites;

public sealed class RevokeInviteHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RevokeInviteHandler _handler;

    public RevokeInviteHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new RevokeInviteHandler(
            _mockContext.Object,
            _mockCurrentUser.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidInvite_RevokesSuccessfully()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockCurrentUser.Setup(x => x.Email).Returns("admin@example.com");

        var command = new RevokeInviteCommand(inviteId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(inviteId);
        result.Status.Should().Be("revoked");
        result.RevokedAt.Should().NotBeNull();

        // Verify SaveChanges was called
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.InviteRevoked,
            currentUserId,
            null,
            "SystemInvite",
            inviteId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInviteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var invites = new List<SystemInvite>();
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new RevokeInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyAccepted_ThrowsBusinessException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);
        invite.Accept(); // Mark as accepted

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new RevokeInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_ALREADY_ACCEPTED");
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyRevoked_ThrowsBusinessException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = TestDataFactory.CreateSystemInvite(
            "invited@example.com",
            "John",
            "Doe",
            invitedBy: inviter.Id);
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);
        invite.Revoke(); // Mark as revoked

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new RevokeInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_ALREADY_REVOKED");
        exception.StatusCode.Should().Be(400);
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
}
