using ExoAuth.Application.Common.Models;
using ExoAuth.Infrastructure.Caching;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace ExoAuth.UnitTests.Services;

public sealed class RateLimitServiceTests
{
    private readonly Mock<IRedisConnectionFactory> _mockRedisFactory;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RateLimitService>> _mockLogger;
    private RateLimitSettings _settings;

    public RateLimitServiceTests()
    {
        _mockRedisFactory = new Mock<IRedisConnectionFactory>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RateLimitService>>();

        _mockRedisFactory.Setup(x => x.GetDatabaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDatabase.Object);

        _settings = new RateLimitSettings
        {
            Enabled = true,
            Presets = new Dictionary<string, RateLimitPreset>
            {
                ["default"] = new() { PerMinute = 100, PerHour = 1000 },
                ["login"] = new() { PerMinute = 5, PerHour = 20 },
                ["sensitive"] = new() { PerMinute = 10, PerHour = 50 }
            },
            AutoBlacklist = new AutoBlacklistSettings
            {
                Enabled = true,
                ViolationThreshold = 5,
                WithinMinutes = 10,
                BlockDurationMinutes = 60
            }
        };
    }

    private RateLimitService CreateService(RateLimitSettings? settings = null)
    {
        var options = Options.Create(settings ?? _settings);
        return new RateLimitService(_mockRedisFactory.Object, options, _mockLogger.Object);
    }

    #region GetPreset Tests

    [Fact]
    public void GetPreset_WhenPresetExists_ReturnsPreset()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetPreset("login");

        // Assert
        result.Should().NotBeNull();
        result!.PerMinute.Should().Be(5);
        result.PerHour.Should().Be(20);
    }

    [Fact]
    public void GetPreset_WhenPresetDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetPreset("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("default", 100, 1000)]
    [InlineData("login", 5, 20)]
    [InlineData("sensitive", 10, 50)]
    public void GetPreset_ReturnsCorrectValues(string presetName, int expectedPerMinute, int expectedPerHour)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetPreset(presetName);

        // Assert
        result.Should().NotBeNull();
        result!.PerMinute.Should().Be(expectedPerMinute);
        result.PerHour.Should().Be(expectedPerHour);
    }

    #endregion

    #region CheckRateLimitAsync Tests

    [Fact]
    public async Task CheckRateLimitAsync_WhenDisabled_ReturnsAllowed()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            Enabled = false,
            Presets = new Dictionary<string, RateLimitPreset>()
        };
        var service = CreateService(settings);

        // Act
        var result = await service.CheckRateLimitAsync("login", "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.Remaining.Should().Be(int.MaxValue);
        result.Limit.Should().Be(int.MaxValue);

        // Verify Redis was not called
        _mockRedisFactory.Verify(x => x.GetDatabaseAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithUnknownPreset_UsesDefaultPreset()
    {
        // Arrange
        var service = CreateService();

        // Setup Redis to allow the request
        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 1, 99 }));

        // Act
        var result = await service.CheckRateLimitAsync("unknown-preset", "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithUserId_UsesUserIdInKey()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        RedisKey[]? capturedKeys = null;

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) => capturedKeys = keys)
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 1, 99 }));

        // Act
        await service.CheckRateLimitAsync("login", "192.168.1.1", userId);

        // Assert
        capturedKeys.Should().NotBeNull();
        capturedKeys![0].ToString().Should().Contain($"user:{userId}");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithoutUserId_UsesIpAddressInKey()
    {
        // Arrange
        var service = CreateService();
        RedisKey[]? capturedKeys = null;

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, _, _) => capturedKeys = keys)
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 1, 99 }));

        // Act
        await service.CheckRateLimitAsync("login", "192.168.1.100");

        // Assert
        capturedKeys.Should().NotBeNull();
        capturedKeys![0].ToString().Should().Contain("ip:192.168.1.100");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenAllowed_ReturnsAllowedResult()
    {
        // Arrange
        var service = CreateService();

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 1, 4 })); // Allowed, 4 remaining

        // Act
        var result = await service.CheckRateLimitAsync("login", "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.Remaining.Should().Be(4);
        result.Limit.Should().Be(5); // login preset has PerMinute = 5
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenExceeded_ReturnsExceededResult()
    {
        // Arrange
        var service = CreateService();
        var resetAtMs = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 0, 0, resetAtMs })); // Exceeded

        // Act
        var result = await service.CheckRateLimitAsync("login", "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
        result.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    #endregion

    #region RecordViolationAsync Tests

    [Fact]
    public async Task RecordViolationAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var settings = new RateLimitSettings
        {
            Enabled = true,
            Presets = new Dictionary<string, RateLimitPreset>(),
            AutoBlacklist = new AutoBlacklistSettings { Enabled = false }
        };
        var service = CreateService(settings);

        // Act
        var result = await service.RecordViolationAsync("192.168.1.1");

        // Assert
        result.Should().BeFalse();

        // Verify Redis was not called
        _mockRedisFactory.Verify(x => x.GetDatabaseAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordViolationAsync_WhenUnderThreshold_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        var mockTransaction = new Mock<ITransaction>();
        _mockDatabase.Setup(x => x.CreateTransaction(It.IsAny<object>()))
            .Returns(mockTransaction.Object);
        mockTransaction.Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetLengthAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(3); // Under threshold of 5

        // Act
        var result = await service.RecordViolationAsync("192.168.1.1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RecordViolationAsync_WhenThresholdReached_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        var mockTransaction = new Mock<ITransaction>();
        _mockDatabase.Setup(x => x.CreateTransaction(It.IsAny<object>()))
            .Returns(mockTransaction.Object);
        mockTransaction.Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetLengthAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(5); // At threshold of 5

        _mockDatabase.Setup(x => x.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.RecordViolationAsync("192.168.1.1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RecordViolationAsync_WhenThresholdExceeded_CleansUpViolationTracking()
    {
        // Arrange
        var service = CreateService();

        var mockTransaction = new Mock<ITransaction>();
        _mockDatabase.Setup(x => x.CreateTransaction(It.IsAny<object>()))
            .Returns(mockTransaction.Object);
        mockTransaction.Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetLengthAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<Exclude>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(6); // Above threshold

        // Act
        await service.RecordViolationAsync("192.168.1.1");

        // Assert
        _mockDatabase.Verify(x => x.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().Contains("ratelimit:violation:192.168.1.1")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CheckRateLimitAsync_WhenMinuteWindowExceeds_DoesNotCheckHourWindow()
    {
        // Arrange
        var service = CreateService();
        var callCount = 0;
        var resetAtMs = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Callback(() => callCount++)
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 0, 0, resetAtMs })); // Always exceed

        // Act
        await service.CheckRateLimitAsync("login", "192.168.1.1");

        // Assert - should only call once (minute window) and stop
        callCount.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CheckRateLimitAsync_WithEmptyPresetName_UsesDefault(string presetName)
    {
        // Arrange
        var service = CreateService();

        _mockDatabase.Setup(x => x.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new RedisValue[] { 1, 99 }));

        // Act
        var result = await service.CheckRateLimitAsync(presetName, "192.168.1.1");

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    #endregion
}
