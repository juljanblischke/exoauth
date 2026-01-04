using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Queries.GetUserTrustedDevices;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class GetUserTrustedDevicesHandlerTests
{
    private readonly Mock<ITrustedDeviceService> _mockTrustedDeviceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetUserTrustedDevicesHandler _handler;
    private readonly List<SystemUser> _users;

    public GetUserTrustedDevicesHandlerTests()
    {
        _mockTrustedDeviceService = new Mock<ITrustedDeviceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockContext = MockDbContext.Create();
        _users = new List<SystemUser>();

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        _handler = new GetUserTrustedDevicesHandler(
            _mockTrustedDeviceService.Object,
            _mockCurrentUserService.Object,
            _mockContext.Object);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        var query = new GetUserTrustedDevicesQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsSystemUserNotFoundException()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        // No users in the list

        var query = new GetUserTrustedDevicesQuery(targetUserId);

        // Act & Assert
        await Assert.ThrowsAsync<SystemUserNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithValidUser_ReturnsTrustedDevices()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        var devices = new List<TrustedDevice>
        {
            TestDataFactory.CreateTrustedDeviceWithId(Guid.NewGuid(), targetUserId, "device-1", "Laptop"),
            TestDataFactory.CreateTrustedDeviceWithId(Guid.NewGuid(), targetUserId, "device-2", "Phone")
        };

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var query = new GetUserTrustedDevicesQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.IsCurrent.Should().BeFalse()); // Admin view - never current
    }

    [Fact]
    public async Task Handle_WithNoDevices_ReturnsEmptyList()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustedDevice>());

        var query = new GetUserTrustedDevicesQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsDevicePropertiesCorrectly()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var user = TestDataFactory.CreateSystemUser();
        TestDataFactory.SetEntityId(user, targetUserId);
        _users.Add(user);

        var device = TestDataFactory.CreateTrustedDeviceWithId(
            deviceId, targetUserId, "test-device", "Work Laptop", "Chrome", "Windows");

        _mockCurrentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustedDevice> { device });

        var query = new GetUserTrustedDevicesQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(deviceId);
        dto.DeviceId.Should().Be("test-device");
        dto.Name.Should().Be("Work Laptop");
        dto.Browser.Should().Be("Chrome");
        dto.OperatingSystem.Should().Be("Windows");
        dto.IsCurrent.Should().BeFalse();
    }
}
