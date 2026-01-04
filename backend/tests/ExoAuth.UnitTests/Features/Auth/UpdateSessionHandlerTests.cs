using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.UpdateSession;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class UpdateSessionHandlerTests
{
    private readonly Mock<IDeviceSessionService> _mockSessionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly UpdateSessionHandler _handler;

    public UpdateSessionHandlerTests()
    {
        _mockSessionService = new Mock<IDeviceSessionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new UpdateSessionHandler(
            _mockSessionService.Object,
            _mockCurrentUserService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithValidName_UpdatesSessionName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var newName = "My Work Laptop";

        var session = CreateSession(userId, sessionId, null);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(sessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockSessionService.Setup(x => x.SetSessionNameAsync(sessionId, newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateSessionCommand(sessionId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockSessionService.Verify(x => x.SetSessionNameAsync(sessionId, newName, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.SessionRenamed,
            userId,
            It.IsAny<Guid?>(),
            "DeviceSession",
            sessionId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSession_ThrowsSessionNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(Guid.NewGuid());
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceSession?)null);

        var command = new UpdateSessionCommand(sessionId, "New Name");

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
        var sessionId = Guid.NewGuid();

        var session = CreateSession(otherUserId, sessionId, null);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(Guid.NewGuid());
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new UpdateSessionCommand(sessionId, "Hacker Name");

        // Act & Assert
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new UpdateSessionCommand(Guid.NewGuid(), "Name");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedSessionDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();

        var session = CreateSession(userId, sessionId, "Chrome on Windows");

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetSessionByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new UpdateSessionCommand(sessionId, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
        result.IsCurrent.Should().BeFalse();
    }

    private static DeviceSession CreateSession(Guid userId, Guid sessionId, string? deviceName)
    {
        var session = DeviceSession.Create(userId, $"device-{sessionId}", deviceName, null, "Mozilla/5.0", "127.0.0.1");
        session.SetDeviceInfo("Chrome", "120", "Windows", "10", "Desktop");

        var idField = typeof(DeviceSession).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(session, sessionId);

        return session;
    }
}
