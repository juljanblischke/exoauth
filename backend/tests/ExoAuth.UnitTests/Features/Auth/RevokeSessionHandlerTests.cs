using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RevokeSession;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class RevokeSessionHandlerTests
{
    private readonly Mock<IDeviceSessionService> _mockSessionService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RevokeSessionHandler _handler;

    public RevokeSessionHandlerTests()
    {
        _mockSessionService = new Mock<IDeviceSessionService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new RevokeSessionHandler(
            _mockSessionService.Object,
            _mockRevokedSessionService.Object,
            _mockCurrentUserService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidSession_RevokesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var targetSessionId = Guid.NewGuid();

        var session = TestDataFactory.CreateDeviceSession(userId, "device-1");
        SetSessionId(session, targetSessionId);
        SetSessionUserId(session, userId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockSessionService.Setup(x => x.RevokeSessionAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RevokeSessionCommand(targetSessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _mockRevokedSessionService.Verify(x => x.RevokeSessionAsync(targetSessionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.SessionRevoked,
            userId,
            It.IsAny<Guid?>(),
            "DeviceSession",
            targetSessionId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTryingToRevokeCurrentSession_ThrowsCannotRevokeCurrentSessionException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);

        var command = new RevokeSessionCommand(currentSessionId); // Same as current

        // Act & Assert
        await Assert.ThrowsAsync<CannotRevokeCurrentSessionException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockSessionService.Verify(x => x.RevokeSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentSession_ThrowsSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var targetSessionId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceSession?)null);

        var command = new RevokeSessionCommand(targetSessionId);

        // Act & Assert
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithSessionBelongingToOtherUser_ThrowsSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var targetSessionId = Guid.NewGuid();

        var session = TestDataFactory.CreateDeviceSession(otherUserId, "device-1");
        SetSessionId(session, targetSessionId);
        SetSessionUserId(session, otherUserId); // Different user

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new RevokeSessionCommand(targetSessionId);

        // Act & Assert
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new RevokeSessionCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenRevokeReturnsFalse_DoesNotLogAudit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var targetSessionId = Guid.NewGuid();

        var session = TestDataFactory.CreateDeviceSession(userId, "device-1");
        SetSessionId(session, targetSessionId);
        SetSessionUserId(session, userId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockSessionService.Setup(x => x.RevokeSessionAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Already revoked

        var command = new RevokeSessionCommand(targetSessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetSessionId(DeviceSession session, Guid id)
    {
        var backingField = typeof(DeviceSession).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(session, id);
    }

    private static void SetSessionUserId(DeviceSession session, Guid userId)
    {
        var backingField = typeof(DeviceSession).GetField("<UserId>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(session, userId);
    }
}
