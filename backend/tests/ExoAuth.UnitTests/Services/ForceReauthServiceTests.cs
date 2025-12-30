using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class ForceReauthServiceTests
{
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IDeviceSessionService> _mockDeviceSessionService;
    private readonly Mock<ILogger<ForceReauthService>> _mockLogger;
    private readonly ForceReauthService _service;

    public ForceReauthServiceTests()
    {
        _mockCache = new Mock<ICacheService>();
        _mockDeviceSessionService = new Mock<IDeviceSessionService>();
        _mockLogger = new Mock<ILogger<ForceReauthService>>();
        _service = new ForceReauthService(
            _mockCache.Object,
            _mockDeviceSessionService.Object,
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
    public async Task SetFlagForAllSessionsAsync_SetsFlagForEachActiveSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session1 = CreateDeviceSession(userId);
        var session2 = CreateDeviceSession(userId);
        var session3 = CreateDeviceSession(userId);

        _mockDeviceSessionService
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceSession> { session1, session2, session3 });

        // Act
        var result = await _service.SetFlagForAllSessionsAsync(userId);

        // Assert
        result.Should().Be(3);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{session1.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{session2.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(
            $"session:force-reauth:{session3.Id}",
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFlagForAllSessionsAsync_WhenNoActiveSessions_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockDeviceSessionService
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceSession>());

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

    private static DeviceSession CreateDeviceSession(Guid userId)
    {
        return DeviceSession.Create(
            userId,
            Guid.NewGuid().ToString(),
            "Test Device",
            null,
            "Mozilla/5.0",
            "127.0.0.1");
    }
}
