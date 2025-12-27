using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Commands.ResendInvite;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemInvites;

public sealed class ResendInviteHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly ResendInviteHandler _handler;

    public ResendInviteHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockEmailService = new Mock<IEmailService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new ResendInviteHandler(
            _mockContext.Object,
            _mockEmailService.Object,
            _mockCurrentUser.Object,
            _mockUserRepository.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidInvite_ResendsSuccessfully()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var resender = TestDataFactory.CreateSystemUser("admin@example.com", firstName: "Admin", lastName: "User");
        SetEntityId(resender, currentUserId);

        // Create invite that was created more than 5 minutes ago
        var invite = CreateInviteWithOldCreatedAt(
            "invited@example.com",
            "John",
            "Doe",
            inviter.Id,
            TimeSpan.FromMinutes(10)); // Created 10 minutes ago
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockCurrentUser.Setup(x => x.Email).Returns("admin@example.com");

        _mockUserRepository.Setup(x => x.GetByIdAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resender);

        var command = new ResendInviteCommand(inviteId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(inviteId);
        result.ResentAt.Should().NotBeNull();

        // Verify email was sent
        _mockEmailService.Verify(x => x.SendSystemInviteAsync(
            "invited@example.com",
            "John",
            "Admin User",
            It.IsAny<string>(),
            "en",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify SaveChanges was called
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.InviteResent,
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

        var command = new ResendInviteCommand(inviteId);

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

        var invite = CreateInviteWithOldCreatedAt(
            "invited@example.com",
            "John",
            "Doe",
            inviter.Id,
            TimeSpan.FromMinutes(10));
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);
        invite.Accept();

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new ResendInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_ALREADY_ACCEPTED");
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyRevoked_ThrowsBusinessException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite = CreateInviteWithOldCreatedAt(
            "invited@example.com",
            "John",
            "Doe",
            inviter.Id,
            TimeSpan.FromMinutes(10));
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);
        invite.Revoke();

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var command = new ResendInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_ALREADY_REVOKED");
    }

    [Fact]
    public async Task Handle_WhenCooldownNotElapsed_ThrowsBusinessException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        // Create invite that was created just now (within cooldown period)
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

        var command = new ResendInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_RESEND_COOLDOWN");
        exception.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task Handle_WhenResentRecentlyWithinCooldown_ThrowsBusinessException()
    {
        // Arrange
        var inviteId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var resender = TestDataFactory.CreateSystemUser("admin@example.com");
        SetEntityId(resender, currentUserId);

        // Create invite that was created long ago but resent recently
        var invite = CreateInviteWithOldCreatedAt(
            "invited@example.com",
            "John",
            "Doe",
            inviter.Id,
            TimeSpan.FromMinutes(30)); // Created 30 minutes ago
        SetEntityId(invite, inviteId);
        SetInvitedByUser(invite, inviter);

        // Set ResentAt to 2 minutes ago (within cooldown)
        SetResentAt(invite, DateTime.UtcNow.AddMinutes(-2));

        var invites = new List<SystemInvite> { invite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resender);

        var command = new ResendInviteCommand(inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.ErrorCode.Should().Be("INVITE_RESEND_COOLDOWN");
        exception.StatusCode.Should().Be(429);
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

    private static void SetResentAt(SystemInvite invite, DateTime resentAt)
    {
        var field = typeof(SystemInvite)
            .GetField("<ResentAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(invite, resentAt);
    }

    private static SystemInvite CreateInviteWithOldCreatedAt(
        string email,
        string firstName,
        string lastName,
        Guid invitedBy,
        TimeSpan age)
    {
        var invite = TestDataFactory.CreateSystemInvite(email, firstName, lastName, invitedBy: invitedBy);

        // Set CreatedAt to a past time
        var createdAtField = typeof(SystemInvite).BaseType?
            .GetField("<CreatedAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        createdAtField?.SetValue(invite, DateTime.UtcNow.Subtract(age));

        return invite;
    }
}
