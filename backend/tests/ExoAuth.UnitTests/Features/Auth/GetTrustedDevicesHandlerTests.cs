using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Queries.GetTrustedDevices;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class GetTrustedDevicesHandlerTests
{
    private readonly Mock<ITrustedDeviceService> _mockTrustedDeviceService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetTrustedDevicesHandler _handler;
    private readonly List<DeviceSession> _deviceSessions;

    public GetTrustedDevicesHandlerTests()
    {
        _mockTrustedDeviceService = new Mock<ITrustedDeviceService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockContext = MockDbContext.Create();
        _deviceSessions = new List<DeviceSession>();

        _mockContext.Setup(x => x.DeviceSessions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_deviceSessions).Object);

        _handler = new GetTrustedDevicesHandler(
            _mockTrustedDeviceService.Object,
            _mockCurrentUserService.Object,
            _mockContext.Object);
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        var query = new GetTrustedDevicesQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithValidUser_ReturnsTrustedDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId1 = Guid.NewGuid();
        var deviceId2 = Guid.NewGuid();

        var devices = new List<TrustedDevice>
        {
            TestDataFactory.CreateTrustedDeviceWithId(deviceId1, userId, "device-1", "My Laptop", "Chrome"),
            TestDataFactory.CreateTrustedDeviceWithId(deviceId2, userId, "device-2", null, "Firefox")
        };

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var query = new GetTrustedDevicesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("My Laptop");
        result.All(d => d.IsCurrent).Should().BeFalse(); // No current session
    }

    [Fact]
    public async Task Handle_WithCurrentSession_MarksCurrentDeviceCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentDeviceId = Guid.NewGuid();
        var otherDeviceId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var devices = new List<TrustedDevice>
        {
            TestDataFactory.CreateTrustedDeviceWithId(currentDeviceId, userId, "device-1"),
            TestDataFactory.CreateTrustedDeviceWithId(otherDeviceId, userId, "device-2")
        };

        // Create session linked to currentDeviceId
        var session = TestDataFactory.CreateDeviceSession(userId, "device-1");
        SetSessionId(session, sessionId);
        SetSessionTrustedDeviceId(session, currentDeviceId);
        _deviceSessions.Add(session);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(sessionId);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var query = new GetTrustedDevicesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First(d => d.Id == currentDeviceId).IsCurrent.Should().BeTrue();
        result.First(d => d.Id == otherDeviceId).IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNoDevices_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustedDevice>());

        var query = new GetTrustedDevicesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsDevicePropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateTrustedDeviceWithId(
            deviceId, userId, "test-device", "Work Laptop", "Chrome", "Windows");

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockTrustedDeviceService.Setup(x => x.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustedDevice> { device });

        var query = new GetTrustedDevicesQuery();

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
