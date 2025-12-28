using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RevokeAllSessions;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class RevokeAllSessionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IDeviceSessionService> _mockSessionService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RevokeAllSessionsHandler _handler;

    public RevokeAllSessionsHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockSessionService = new Mock<IDeviceSessionService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _handler = new RevokeAllSessionsHandler(
            _mockContext.Object,
            _mockSessionService.Object,
            _mockRevokedSessionService.Object,
            _mockCurrentUserService.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithMultipleSessions_RevokesAllExceptCurrent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var otherSession1Id = Guid.NewGuid();
        var otherSession2Id = Guid.NewGuid();

        var sessions = new List<DeviceSession>
        {
            CreateSession(userId, currentSessionId, false),
            CreateSession(userId, otherSession1Id, false),
            CreateSession(userId, otherSession2Id, false)
        };

        var mockDbSet = MockDbContext.CreateAsyncMockDbSet(sessions);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockDbSet.Object);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.RevokeAllSessionsExceptAsync(userId, currentSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = new RevokeAllSessionsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RevokedCount.Should().Be(2);
        _mockRevokedSessionService.Verify(x => x.RevokeSessionsAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2 && !ids.Contains(currentSessionId)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.SessionRevokedAll,
            userId,
            It.IsAny<Guid?>(),
            "DeviceSession",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoOtherSessions_ReturnsZeroCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();

        var sessions = new List<DeviceSession>
        {
            CreateSession(userId, currentSessionId, false) // Only current session
        };

        var mockDbSet = MockDbContext.CreateAsyncMockDbSet(sessions);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockDbSet.Object);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.RevokeAllSessionsExceptAsync(userId, currentSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new RevokeAllSessionsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RevokedCount.Should().Be(0);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new RevokeAllSessionsCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ExcludesAlreadyRevokedSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var activeSessionId = Guid.NewGuid();
        var revokedSessionId = Guid.NewGuid();

        var sessions = new List<DeviceSession>
        {
            CreateSession(userId, currentSessionId, false),
            CreateSession(userId, activeSessionId, false),
            CreateSession(userId, revokedSessionId, true) // Already revoked
        };

        var mockDbSet = MockDbContext.CreateAsyncMockDbSet(sessions);
        _mockContext.Setup(x => x.DeviceSessions).Returns(mockDbSet.Object);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.RevokeAllSessionsExceptAsync(userId, currentSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RevokeAllSessionsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RevokedCount.Should().Be(1);
        // Only the active non-current session should be in the revoke list
        _mockRevokedSessionService.Verify(x => x.RevokeSessionsAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.Contains(activeSessionId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DeviceSession CreateSession(Guid userId, Guid sessionId, bool isRevoked)
    {
        var session = DeviceSession.Create(userId, $"device-{sessionId}", null, null, null, null);

        var idField = typeof(DeviceSession).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(session, sessionId);

        if (isRevoked)
        {
            session.Revoke();
        }

        return session;
    }
}
