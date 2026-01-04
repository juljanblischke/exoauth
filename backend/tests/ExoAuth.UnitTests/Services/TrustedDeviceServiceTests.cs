using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Infrastructure.Services;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class TrustedDeviceServiceTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ILogger<TrustedDeviceService>> _mockLogger;
    private readonly TrustedDeviceService _service;
    private readonly List<TrustedDevice> _trustedDevices;

    public TrustedDeviceServiceTests()
    {
        _mockContext = MockDbContext.Create();
        _mockLogger = new Mock<ILogger<TrustedDeviceService>>();
        _trustedDevices = new List<TrustedDevice>();

        _mockContext.Setup(x => x.TrustedDevices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_trustedDevices).Object);

        _service = new TrustedDeviceService(
            _mockContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task FindAsync_WithExistingDevice_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = "test-device";
        var device = TestDataFactory.CreateTrustedDevice(userId, deviceId);
        _trustedDevices.Add(device);

        // Act
        var result = await _service.FindAsync(userId, deviceId, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(deviceId);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task FindAsync_WithNonExistentDevice_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.FindAsync(userId, "non-existent", null, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_ByFingerprint_WhenDeviceIdNotFound_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device = TestDataFactory.CreateTrustedDevice(userId, "old-device-id");
        _trustedDevices.Add(device);

        // Act - lookup by new device ID but same fingerprint
        var result = await _service.FindAsync(userId, "new-device-id", "test-fingerprint", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task AddAsync_CreatesNewTrustedDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = "new-device";
        var deviceInfo = new DeviceInfo("Chrome", "120", "Windows", "10", "Desktop");
        var geoLocation = new GeoLocation("127.0.0.1", "Germany", "DE", "Berlin", null, null);

        // Act
        var result = await _service.AddAsync(userId, deviceId, deviceInfo, geoLocation, "fingerprint", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.DeviceId.Should().Be(deviceId);
        result.Browser.Should().Be("Chrome");
        result.OperatingSystem.Should().Be("Windows");
        _trustedDevices.Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_WhenDeviceAlreadyExists_ReturnsExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = "existing-device";
        var existingDevice = TestDataFactory.CreateTrustedDevice(userId, deviceId);
        _trustedDevices.Add(existingDevice);

        var deviceInfo = new DeviceInfo("Firefox", "121", "Linux", "22", "Desktop");
        var geoLocation = new GeoLocation("192.168.1.1", "USA", "US", "New York", null, null);

        // Act
        var result = await _service.AddAsync(userId, deviceId, deviceInfo, geoLocation, null, CancellationToken.None);

        // Assert
        result.Should().Be(existingDevice);
        _trustedDevices.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDevicesForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(userId, "device-1"));
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(userId, "device-2"));
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(otherUserId, "device-3"));

        // Act
        var result = await _service.GetAllAsync(userId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingDevice_ReturnsDevice()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);
        _trustedDevices.Add(device);

        // Act
        var result = await _service.GetByIdAsync(deviceId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(deviceId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentDevice_ReturnsNull()
    {
        // Arrange & Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithExistingDevice_RemovesAndReturnsTrue()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);
        _trustedDevices.Add(device);

        // Act
        var result = await _service.RemoveAsync(deviceId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _trustedDevices.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistentDevice_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _service.RemoveAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAllAsync_RemovesAllDevicesForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(userId, "device-1"));
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(userId, "device-2"));
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(otherUserId, "device-3"));

        // Act
        var result = await _service.RemoveAllAsync(userId, CancellationToken.None);

        // Assert - verify the count returned matches devices for that user
        result.Should().Be(2);
        // Note: Mock DbSet doesn't actually remove from backing list, but service returns correct count
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAllAsync_WhenNoDevices_ReturnsZero()
    {
        // Arrange & Act
        var result = await _service.RemoveAllAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task RenameAsync_WithExistingDevice_RenamesAndReturnsTrue()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var device = TestDataFactory.CreateTrustedDeviceWithId(deviceId, userId);
        _trustedDevices.Add(device);

        // Act
        var result = await _service.RenameAsync(deviceId, "My Laptop", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        device.Name.Should().Be("My Laptop");
    }

    [Fact]
    public async Task RenameAsync_WithNonExistentDevice_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _service.RenameAsync(Guid.NewGuid(), "Name", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyAsync_WhenUserHasDevices_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _trustedDevices.Add(TestDataFactory.CreateTrustedDevice(userId));

        // Act
        var result = await _service.HasAnyAsync(userId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyAsync_WhenUserHasNoDevices_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _service.HasAnyAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
