using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class PermissionCacheServiceTests
{
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<ILogger<PermissionCacheService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly PermissionCacheService _service;

    public PermissionCacheServiceTests()
    {
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<PermissionCacheService>>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:PermissionCacheTtlMinutes"] = "60"
            })
            .Build();

        _service = new PermissionCacheService(
            _mockCache.Object,
            _configuration,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsNullWhenNotCached()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        // Act
        var result = await _service.GetPermissionsAsync(userId);

        // Assert
        result.Should().BeNull();
        _mockCache.Verify(x => x.GetAsync<List<string>>($"user:permissions:{userId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsCachedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "system:users:read", "system:users:create" };
        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        // Act
        var result = await _service.GetPermissionsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task SetPermissionsAsync_CachesWithCorrectTtl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "system:users:read" };

        // Act
        await _service.SetPermissionsAsync(userId, permissions);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            $"user:permissions:{userId}",
            permissions,
            TimeSpan.FromMinutes(60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesCacheEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.InvalidateAsync(userId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync($"user:permissions:{userId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAllAsync_DeletesByPattern()
    {
        // Act
        await _service.InvalidateAllAsync();

        // Assert
        _mockCache.Verify(x => x.DeleteByPatternAsync("user:permissions:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetPermissionsAsync_ReturnsCachedWhenAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedPermissions = new List<string> { "system:users:read" };
        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPermissions);

        var fetchCalled = false;
        Func<Task<List<string>>> fetchFromDb = () =>
        {
            fetchCalled = true;
            return Task.FromResult(new List<string> { "fresh:permission" });
        };

        // Act
        var result = await _service.GetOrSetPermissionsAsync(userId, fetchFromDb);

        // Assert
        result.Should().BeEquivalentTo(cachedPermissions);
        fetchCalled.Should().BeFalse(); // DB fetch should not be called
    }

    [Fact]
    public async Task GetOrSetPermissionsAsync_FetchesFromDbOnCacheMiss()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var freshPermissions = new List<string> { "system:users:read", "system:users:create" };

        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        var fetchCalled = false;
        Func<Task<List<string>>> fetchFromDb = () =>
        {
            fetchCalled = true;
            return Task.FromResult(freshPermissions);
        };

        // Act
        var result = await _service.GetOrSetPermissionsAsync(userId, fetchFromDb);

        // Assert
        result.Should().BeEquivalentTo(freshPermissions);
        fetchCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrSetPermissionsAsync_CachesAfterFetching()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var freshPermissions = new List<string> { "system:users:read" };

        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        Func<Task<List<string>>> fetchFromDb = () => Task.FromResult(freshPermissions);

        // Act
        await _service.GetOrSetPermissionsAsync(userId, fetchFromDb);

        // Assert - verify it was cached
        _mockCache.Verify(x => x.SetAsync(
            $"user:permissions:{userId}",
            freshPermissions,
            TimeSpan.FromMinutes(60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_UsesDefaultTtlWhenNotConfigured()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act - should not throw
        var service = new PermissionCacheService(
            _mockCache.Object,
            emptyConfig,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrSetPermissionsAsync_WithEmptyPermissions_StillCaches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyPermissions = new List<string>();

        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        Func<Task<List<string>>> fetchFromDb = () => Task.FromResult(emptyPermissions);

        // Act
        var result = await _service.GetOrSetPermissionsAsync(userId, fetchFromDb);

        // Assert
        result.Should().BeEmpty();
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.Is<List<string>>(list => list.Count == 0),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsAsync_UsesCorrectKeyFormat()
    {
        // Arrange
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        _mockCache.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        // Act
        await _service.GetPermissionsAsync(userId);

        // Assert
        _mockCache.Verify(x => x.GetAsync<List<string>>(
            "user:permissions:11111111-2222-3333-4444-555555555555",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
