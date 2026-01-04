using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.RemoveUserTrustedDevice;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class RemoveUserTrustedDeviceHandlerTests
{
    private readonly Mock<ITrustedDeviceService> _mockTrustedDeviceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RemoveUserTrustedDeviceHandler _handler;
    private readonly List<SystemUser> _users;

    public RemoveUserTrustedDeviceHandlerTests()
    {
        _mockTrustedDeviceService = new Mock<ITrustedDeviceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _users = new List<SystemUser>();

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        _handler = new RemoveUserTrustedDeviceHandler(
            _mockTrustedDeviceService.Object,
            _mockCurrentUserService.Object,
            _mockContext.Object,
            _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        var command = new RemoveUserTrustedDeviceCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        // No users in the list

        var command = new RemoveUserTrustedDeviceCommand(targetUserId, deviceId);

        // Act & Assert
        await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenDeviceNotFound_ThrowsDeviceNotFoundException()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustedDevice?)null);

        var command = new RemoveUserTrustedDeviceCommand(targetUserId, deviceId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None).AsTask();

        // Assert
        var exception = await Assert.ThrowsAsync<AuthException>(act);
        exception.ErrorCode.Should().Be("DEVICE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenDeviceBelongsToOtherUser_ThrowsDeviceNotFoundException()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, otherUserId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var command = new RemoveUserTrustedDeviceCommand(targetUserId, deviceId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None).AsTask();

        // Assert
        var exception = await Assert.ThrowsAsync<AuthException>(act);
        exception.ErrorCode.Should().Be("DEVICE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithValidDevice_RemovesDevice()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, targetUserId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockTrustedDeviceService.Setup(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveUserTrustedDeviceCommand(targetUserId, deviceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTrustedDeviceService.Verify(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidDevice_LogsAuditEvent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, targetUserId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockTrustedDeviceService.Setup(x => x.RemoveAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RemoveUserTrustedDeviceCommand(targetUserId, deviceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.TrustedDeviceRemovedByAdmin,
            adminUserId,
            targetUserId,
            "TrustedDevice",
            deviceId,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
