using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class ForceReauthServiceTests
{
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<ILogger<ForceReauthService>> _mockLogger;
    private readonly ForceReauthService _service;

    public ForceReauthServiceTests()
    {
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ForceReauthService>>();
        _service = new ForceReauthService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SetFlagAsync_StoresFlagInCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"user:force-reauth:{userId}";

        // Act
        await _service.SetFlagAsync(userId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            expectedKey,
            It.IsAny<object>(),
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasFlagAsync_WhenFlagExists_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"user:force-reauth:{userId}";

        _mockCache.Setup(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasFlagAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockCache.Verify(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasFlagAsync_WhenFlagDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"user:force-reauth:{userId}";

        _mockCache.Setup(x => x.ExistsAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.HasFlagAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClearFlagAsync_RemovesFlagFromCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = $"user:force-reauth:{userId}";

        // Act
        await _service.ClearFlagAsync(userId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFlagAsync_UsesCancellationToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        await _service.SetFlagAsync(userId, cts.Token);

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
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        _mockCache.Setup(x => x.ExistsAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync(false);

        // Act
        await _service.HasFlagAsync(userId, cts.Token);

        // Assert
        _mockCache.Verify(x => x.ExistsAsync(It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task ClearFlagAsync_UsesCancellationToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        await _service.ClearFlagAsync(userId, cts.Token);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task SetFlagAsync_UsesCorrectKeyPrefix()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.SetFlagAsync(userId);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            It.Is<string>(key => key.StartsWith("user:force-reauth:")),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
