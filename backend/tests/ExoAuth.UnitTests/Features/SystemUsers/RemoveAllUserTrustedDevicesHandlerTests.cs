using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Commands.RemoveAllUserTrustedDevices;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class RemoveAllUserTrustedDevicesHandlerTests
{
    private readonly Mock<ITrustedDeviceService> _mockTrustedDeviceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RemoveAllUserTrustedDevicesHandler _handler;
    private readonly List<SystemUser> _users;

    public RemoveAllUserTrustedDevicesHandlerTests()
    {
        _mockTrustedDeviceService = new Mock<ITrustedDeviceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _users = new List<SystemUser>();

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        _handler = new RemoveAllUserTrustedDevicesHandler(
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
        var command = new RemoveAllUserTrustedDevicesCommand(Guid.NewGuid());

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

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        // No users in the list

        var command = new RemoveAllUserTrustedDevicesCommand(targetUserId);

        // Act & Assert
        await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithValidUser_RemovesAllDevices()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.RemoveAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new RemoveAllUserTrustedDevicesCommand(targetUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(3);
        _mockTrustedDeviceService.Verify(x => x.RemoveAllAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDevicesRemoved_LogsAuditEvent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.RemoveAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = new RemoveAllUserTrustedDevicesCommand(targetUserId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.TrustedDevicesRemovedByAdmin,
            adminUserId,
            targetUserId,
            "TrustedDevice",
            null,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoDevices_ReturnsZeroWithoutAuditLog()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.RemoveAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new RemoveAllUserTrustedDevicesCommand(targetUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
