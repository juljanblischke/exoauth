using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RemoveTrustedDevice;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class RemoveTrustedDeviceHandlerTests
{
    private readonly Mock<ITrustedDeviceService> _mockTrustedDeviceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RemoveTrustedDeviceHandler _handler;
    private readonly List<DeviceSession> _deviceSessions;

    public RemoveTrustedDeviceHandlerTests()
    {
        _mockTrustedDeviceService = new Mock<ITrustedDeviceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _deviceSessions = new List<DeviceSession>();

        _mockContext.Setup(x => x.DeviceSessions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_deviceSessions).Object);

        _handler = new RemoveTrustedDeviceHandler(
            _mockTrustedDeviceService.Object,
            _mockCurrentUserService.Object,
            _mockContext.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        var command = new RemoveTrustedDeviceCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNonExistentDevice_ThrowsDeviceNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustedDevice?)null);

        var command = new RemoveTrustedDeviceCommand(deviceId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None).AsTask();

        // Assert
        var exception = await Assert.ThrowsAsync<AuthException>(act);
        exception.ErrorCode.Should().Be("DEVICE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithDeviceBelongingToOtherUser_ThrowsDeviceNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, otherUserId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var command = new RemoveTrustedDeviceCommand(deviceId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None).AsTask();

        // Assert
        var exception = await Assert.ThrowsAsync<AuthException>(act);
        exception.ErrorCode.Should().Be("DEVICE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithCurrentDevice_ThrowsCannotRemoveCurrentDeviceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);

        // Create session linked to the device being removed
        var session = TestDataFactory.CreateDeviceSession(userId);
        SetSessionId(session, sessionId);
        SetSessionTrustedDeviceId(session, deviceId);
        _deviceSessions.Add(session);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(sessionId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var command = new RemoveTrustedDeviceCommand(deviceId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None).AsTask();

        // Assert
        var exception = await Assert.ThrowsAsync<AuthException>(act);
        exception.ErrorCode.Should().Be("CANNOT_REMOVE_CURRENT_DEVICE");
    }

    [Fact]
    public async Task Handle_WithValidDevice_RemovesDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockTrustedDeviceService.Setup(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveTrustedDeviceCommand(deviceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTrustedDeviceService.Verify(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidDevice_LogsAuditEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockTrustedDeviceService.Setup(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveTrustedDeviceCommand(deviceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.TrustedDeviceRemoved,
            userId,
            It.IsAny<Guid?>(),
            "TrustedDevice",
            deviceId,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrentDevice_AllowsRemoval()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceIdToRemove = Guid.NewGuid();
        var currentDeviceId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceIdToRemove, userId);

        // Create session linked to a DIFFERENT device
        var session = TestDataFactory.CreateDeviceSession(userId);
        SetSessionId(session, sessionId);
        SetSessionTrustedDeviceId(session, currentDeviceId);
        _deviceSessions.Add(session);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(sessionId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceIdToRemove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockTrustedDeviceService.Setup(x => x.RemoveAsync(deviceIdToRemove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveTrustedDeviceCommand(deviceIdToRemove);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTrustedDeviceService.Verify(x => x.RemoveAsync(deviceIdToRemove, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void SetSessionId(DeviceSession session, Guid id)
    {
        var backingField = typeof(DeviceSession).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(session, id);
    }

    private static void SetSessionTrustedDeviceId(DeviceSession session, Guid? trustedDeviceId)
    {
        var property = typeof(DeviceSession).GetProperty("TrustedDeviceId");
        property?.SetValue(session, trustedDeviceId);
    }
}
