using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class BruteForceProtectionServiceTests
{
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<ILogger<BruteForceProtectionService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly BruteForceProtectionService _service;

    public BruteForceProtectionServiceTests()
    {
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<BruteForceProtectionService>>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BruteForce:MaxAttempts"] = "10",
                ["BruteForce:LockoutMinutes"] = "60",
                ["Lockout:ProgressiveDelays:0"] = "0",
                ["Lockout:ProgressiveDelays:1"] = "0",
                ["Lockout:ProgressiveDelays:2"] = "60",
                ["Lockout:ProgressiveDelays:3"] = "120",
                ["Lockout:ProgressiveDelays:4"] = "300",
                ["Lockout:ProgressiveDelays:5"] = "600",
                ["Lockout:ProgressiveDelays:6"] = "900",
                ["Lockout:ProgressiveDelays:7"] = "1800",
                ["Lockout:ProgressiveDelays:8"] = "3600",
                ["Lockout:NotifyAfterSeconds"] = "900"
            })
            .Build();

        _service = new BruteForceProtectionService(
            _mockCache.Object,
            _configuration,
            _mockLogger.Object);
    }

    [Fact]
    public async Task IsBlockedAsync_WhenNotBlocked_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsBlockedAsync(email);

        // Assert
        result.Should().BeFalse();
        _mockCache.Verify(x => x.ExistsAsync("login:blocked:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsBlockedAsync_WhenBlocked_ReturnsTrue()
    {
        // Arrange
        var email = "blocked@example.com";
        _mockCache.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsBlockedAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsBlockedAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";
        _mockCache.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.IsBlockedAsync(email);

        // Assert
        _mockCache.Verify(x => x.ExistsAsync("login:blocked:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_FirstTwoAttempts_NoLockout()
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.Attempts.Should().Be(1);
        result.IsLocked.Should().BeFalse();
        result.LockoutSeconds.Should().Be(0);
        result.LockedUntil.Should().BeNull();
        result.ShouldNotify.Should().BeFalse();

        // Verify blocked flag was NOT set
        _mockCache.Verify(x => x.SetAsync(
            "login:blocked:test@example.com",
            It.IsAny<object>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_ThirdAttempt_60SecondLockout()
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.Attempts.Should().Be(3);
        result.IsLocked.Should().BeTrue();
        result.LockoutSeconds.Should().Be(60);
        result.LockedUntil.Should().NotBeNull();
        result.ShouldNotify.Should().BeFalse(); // 60s < 900s NotifyAfterSeconds

        // Verify blocked flag was set
        _mockCache.Verify(x => x.SetAsync(
            "login:blocked:test@example.com",
            It.IsAny<object>(),
            TimeSpan.FromSeconds(60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_SeventhAttempt_15MinuteLockoutWithNotification()
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.Attempts.Should().Be(7);
        result.IsLocked.Should().BeTrue();
        result.LockoutSeconds.Should().Be(900); // 15 minutes
        result.LockedUntil.Should().NotBeNull();
        result.ShouldNotify.Should().BeTrue(); // 900s >= 900s NotifyAfterSeconds

        // Verify blocked flag was set
        _mockCache.Verify(x => x.SetAsync(
            "login:blocked:test@example.com",
            It.IsAny<object>(),
            TimeSpan.FromSeconds(900),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_MaxAttemptsExceeded_LongLockout()
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // Max attempts reached

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.Attempts.Should().Be(10);
        result.IsLocked.Should().BeTrue();
        result.LockoutSeconds.Should().Be(3600); // 60 minutes (from config)
        result.ShouldNotify.Should().BeTrue();
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.RecordFailedAttemptAsync(email);

        // Assert
        _mockCache.Verify(x => x.IncrementAsync(
            "login:attempts:test@example.com",
            It.IsAny<long>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_RemovesAllKeys()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        await _service.ResetAsync(email);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync("login:attempts:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("login:blocked:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("login:lockout:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";

        // Act
        await _service.ResetAsync(email);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync("login:attempts:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("login:blocked:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("login:lockout:test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRemainingAttemptsAsync_WithNoAttempts_ReturnsFirstLockoutAttempt()
    {
        // Arrange
        var email = "test@example.com";
        // The service uses GetAsync<AttemptsInfo> which is a private nested type.
        // Since nothing is cached, the method returns null by default via Moq's default value provider.

        // Act
        var result = await _service.GetRemainingAttemptsAsync(email);

        // Assert
        // First lockout is at attempt 3 (index 2 in ProgressiveDelays where value > 0)
        result.Should().Be(3);
    }

    [Fact]
    public void Constructor_UsesDefaultValuesWhenNotConfigured()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act - should not throw
        var service = new BruteForceProtectionService(
            _mockCache.Object,
            emptyConfig,
            _mockLogger.Object);

        // Assert - service was created successfully
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1, false, 0)]
    [InlineData(2, false, 0)]
    [InlineData(3, true, 60)]
    [InlineData(4, true, 120)]
    [InlineData(5, true, 300)]
    [InlineData(6, true, 600)]
    [InlineData(7, true, 900)]
    [InlineData(8, true, 1800)]
    [InlineData(9, true, 3600)]
    [InlineData(10, true, 3600)] // Max attempts uses last delay
    [InlineData(15, true, 3600)] // Beyond array uses last delay
    public async Task RecordFailedAttemptAsync_ProgressiveLockout_ReturnsCorrectDelay(
        int attemptCount, bool expectedLocked, int expectedSeconds)
    {
        // Arrange
        var email = "test@example.com";
        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attemptCount);

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.IsLocked.Should().Be(expectedLocked);
        result.LockoutSeconds.Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(60, false)]   // 1 minute - no notification
    [InlineData(120, false)]  // 2 minutes - no notification
    [InlineData(300, false)]  // 5 minutes - no notification
    [InlineData(600, false)]  // 10 minutes - no notification
    [InlineData(900, true)]   // 15 minutes - notification
    [InlineData(1800, true)]  // 30 minutes - notification
    [InlineData(3600, true)]  // 60 minutes - notification
    public async Task RecordFailedAttemptAsync_ShouldNotify_CorrectlyDetermined(
        int lockoutSeconds, bool expectedShouldNotify)
    {
        // Arrange
        var email = "test@example.com";
        // Find attempt number that gives the desired lockout
        var attemptNumber = lockoutSeconds switch
        {
            60 => 3,
            120 => 4,
            300 => 5,
            600 => 6,
            900 => 7,
            1800 => 8,
            3600 => 9,
            _ => 1
        };

        _mockCache.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attemptNumber);

        // Act
        var result = await _service.RecordFailedAttemptAsync(email);

        // Assert
        result.ShouldNotify.Should().Be(expectedShouldNotify);
    }

    [Fact]
    public async Task GetLockoutStatusAsync_WhenNotLocked_ReturnsNull()
    {
        // Arrange
        var email = "test@example.com";
        // No setup needed - Moq returns null by default for reference types

        // Act
        var result = await _service.GetLockoutStatusAsync(email);

        // Assert
        result.Should().BeNull();
    }
}
