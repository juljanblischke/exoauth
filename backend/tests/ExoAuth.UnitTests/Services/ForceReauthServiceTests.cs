using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Infrastructure.Services;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class ForceReauthServiceTests
{
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IDeviceService> _mockDeviceService;
    private readonly Mock<ILogger<ForceReauthService>> _mockLogger;
    private readonly ForceReauthService _service;

    public ForceReauthServiceTests()
    {
        _mockCache = new Mock<ICacheService>();
        _mockDeviceService = new Mock<IDeviceService>();
        _mockLogger = new Mock<ILogger<ForceReauthService>>();
        _service = new ForceReauthService(
            _mockCache.Object,
            _mockDeviceService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SetFlagAsync_StoresFlagInCache_WithSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedKey = $"session:force-reauth:{sessionId}";

        // Act
        await _service.SetFlagAsync(sessionId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            expectedKey,
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFlagForAllSessionsAsync_SetsFlagForEachTrustedDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device1 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var device2 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);
        var device3 = TestDataFactory.CreateDeviceWithId(Guid.NewGuid(), userId);

        _mockDeviceService
            .Setup(x => x.GetTrustedDevicesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device> { device1, device2, device3 });

        // Act
        var result = await _service.SetFlagForAllSessionsAsync(userId);

        // Assert
        result.Should().Be(3);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{device1.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{device2.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{device3.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFlagForAllSessionsAsync_WhenNoTrustedDevices_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockDeviceService
            .Setup(x => x.GetTrustedDevicesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Device>());

        // Act
        var result = await _service.SetFlagForAllSessionsAsync(userId);

        // Assert
        result.Should().Be(0);
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HasFlagAsync_WhenFlagExists_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedKey = $"session:force-reauth:{sessionId}";

        _mockCache.Setup(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasFlagAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockCache.Verify(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasFlagAsync_WhenFlagDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedKey = $"session:force-reauth:{sessionId}";

        _mockCache.Setup(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.HasFlagAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClearFlagAsync_RemovesFlagFromCache()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedKey = $"session:force-reauth:{sessionId}";

        // Act
        await _service.ClearFlagAsync(sessionId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFlagAsync_UsesCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        await _service.SetFlagAsync(sessionId, cts.Token);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            cts.Token), Times.Once);
    }

    [Fact]
    public async Task HasFlagAsync_UsesCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        _mockCache.Setup(x => x.ExistsAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync(false);

        // Act
        await _service.HasFlagAsync(sessionId, cts.Token);

        // Assert
        _mockCache.Verify(x => x.ExistsAsync(It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task ClearFlagAsync_UsesCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        await _service.ClearFlagAsync(sessionId, cts.Token);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task SetFlagAsync_UsesCorrectKeyPrefix()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        await _service.SetFlagAsync(sessionId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            It.Is<string>(key => key.StartsWith("session:force-reauth:")),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
