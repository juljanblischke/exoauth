using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class IpRestrictionServiceTests
{
    private readonly Mock<IAppDbContext> _mockDbContext;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<IpRestrictionService>> _mockLogger;
    private RateLimitSettings _settings;
    private readonly DateTime _testNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private List<IpRestriction> _restrictions;

    public IpRestrictionServiceTests()
    {
        _mockDbContext = new Mock<IAppDbContext>();
        _mockCache = new Mock<ICacheService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<IpRestrictionService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_testNow);

        // Default empty list
        _restrictions = new List<IpRestriction>();
        SetupDbSet(_restrictions);

        // By default Moq returns null for unconfigured GetAsync calls (cache miss)
        // so service will load from database

        _settings = new RateLimitSettings
        {
            Enabled = true,
            Presets = new Dictionary<string, RateLimitPreset>
            {
                ["default"] = new() { PerMinute = 100, PerHour = 1000 }
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

    private void SetupDbSet(List<IpRestriction> data)
    {
        var mockDbSet = CreateMockDbSet(data);
        _mockDbContext.Setup(x => x.IpRestrictions).Returns(mockDbSet.Object);
    }

    private IpRestrictionService CreateService(RateLimitSettings? settings = null)
    {
        var options = Options.Create(settings ?? _settings);
        return new IpRestrictionService(
            _mockDbContext.Object,
            _mockCache.Object,
            _mockDateTimeProvider.Object,
            options,
            _mockLogger.Object);
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);

        return mockSet;
    }

    #region CheckIpAsync Tests

    [Fact]
    public async Task CheckIpAsync_WithInvalidIpFormat_ReturnsNeitherBlacklistedNorWhitelisted()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("invalid-ip");

        // Assert
        result.Should().NotBeNull();
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeFalse();
        result.Reason.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task CheckIpAsync_WhenIpIsBlacklisted_ReturnsBlacklisted()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Malicious activity",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.Should().NotBeNull();
        result.IsBlacklisted.Should().BeTrue();
        result.IsWhitelisted.Should().BeFalse();
        result.Reason.Should().Be("Malicious activity");
    }

    [Fact]
    public async Task CheckIpAsync_WhenIpIsWhitelisted_ReturnsWhitelisted()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.1.50",
            IpRestrictionType.Whitelist,
            "Trusted internal server",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeTrue();
        result.Reason.Should().Be("Trusted internal server");
    }

    [Fact]
    public async Task CheckIpAsync_WhenIpMatchesCidrBlacklist_ReturnsBlacklisted()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "10.0.0.0/8",
            IpRestrictionType.Blacklist,
            "Blocked network range",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("10.50.100.200");

        // Assert
        result.IsBlacklisted.Should().BeTrue();
        result.Reason.Should().Be("Blocked network range");
    }

    [Fact]
    public async Task CheckIpAsync_WhenIpMatchesCidrWhitelist_ReturnsWhitelisted()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.0.0/16",
            IpRestrictionType.Whitelist,
            "Internal network",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.100.50");

        // Assert
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeTrue();
        result.Reason.Should().Be("Internal network");
    }

    [Fact]
    public async Task CheckIpAsync_BlacklistTakesPriorityOverWhitelist()
    {
        // Arrange
        var blacklistEntry = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Compromised machine",
            null,
            Guid.NewGuid());
        var whitelistEntry = IpRestriction.CreateManual(
            "192.168.0.0/16",
            IpRestrictionType.Whitelist,
            "Internal network",
            null,
            Guid.NewGuid());
        _restrictions.Add(blacklistEntry);
        _restrictions.Add(whitelistEntry);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.IsBlacklisted.Should().BeTrue();
        result.IsWhitelisted.Should().BeFalse();
    }

    [Fact]
    public async Task CheckIpAsync_ExpiredBlacklistEntry_IsIgnored()
    {
        // Arrange
        var expiredEntry = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Expired block",
            _testNow.AddMinutes(-10)); // Expired 10 minutes ago
        _restrictions.Add(expiredEntry);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeFalse();
    }

    [Fact]
    public async Task CheckIpAsync_ExpiredWhitelistEntry_IsIgnored()
    {
        // Arrange
        var expiredEntry = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Whitelist,
            "Expired whitelist",
            _testNow.AddMinutes(-5)); // Expired 5 minutes ago
        _restrictions.Add(expiredEntry);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeFalse();
    }

    [Fact]
    public async Task CheckIpAsync_WhenNotInAnyList_ReturnsNeitherBlacklistedNorWhitelisted()
    {
        // Arrange (no restrictions added)
        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.IsBlacklisted.Should().BeFalse();
        result.IsWhitelisted.Should().BeFalse();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task CheckIpAsync_ReturnsExpiresAt_WhenSet()
    {
        // Arrange
        var expiresAt = _testNow.AddHours(1);
        var restriction = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Temporary block",
            expiresAt);
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync("192.168.1.100");

        // Assert
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task CheckIpAsync_WhenCacheMiss_LoadsFromDatabaseAndCaches()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.CheckIpAsync("192.168.1.100");

        // Assert - Verify cache was populated (SetAsync called for both blacklist and whitelist)
        _mockCache.Verify(x => x.SetAsync(
            "ip:blacklist",
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockCache.Verify(x => x.SetAsync(
            "ip:whitelist",
            It.IsAny<object>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("192.168.1.1", "192.168.1.0/24", true)]
    [InlineData("192.168.2.1", "192.168.1.0/24", false)]
    [InlineData("10.0.0.1", "10.0.0.0/8", true)]
    [InlineData("11.0.0.1", "10.0.0.0/8", false)]
    [InlineData("172.16.0.1", "172.16.0.0/12", true)]
    [InlineData("172.32.0.1", "172.16.0.0/12", false)]
    public async Task CheckIpAsync_CidrMatching_WorksCorrectly(string ipAddress, string cidr, bool shouldMatch)
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            cidr,
            IpRestrictionType.Blacklist,
            "Test CIDR",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var result = await service.CheckIpAsync(ipAddress);

        // Assert
        result.IsBlacklisted.Should().Be(shouldMatch);
    }

    [Fact]
    public async Task CheckIpAsync_ExactIpMatch_WorksCorrectly()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Exact IP match",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        var matchResult = await service.CheckIpAsync("192.168.1.100");

        // Reset and test non-match
        _restrictions.Clear();
        _restrictions.Add(restriction);
        SetupDbSet(_restrictions);
        var nonMatchResult = await service.CheckIpAsync("192.168.1.101");

        // Assert
        matchResult.IsBlacklisted.Should().BeTrue();
        nonMatchResult.IsBlacklisted.Should().BeFalse();
    }

    #endregion

    #region AutoBlacklistAsync Tests

    [Fact]
    public async Task AutoBlacklistAsync_CreatesBlacklistEntry()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.AutoBlacklistAsync("192.168.1.100", "Rate limit exceeded", 60);

        // Assert
        _restrictions.Should().HaveCount(1);
        _restrictions[0].IpAddress.Should().Be("192.168.1.100");
        _restrictions[0].Type.Should().Be(IpRestrictionType.Blacklist);
        _restrictions[0].Reason.Should().Be("Rate limit exceeded");
        _restrictions[0].Source.Should().Be(IpRestrictionSource.Auto);
        _restrictions[0].ExpiresAt.Should().Be(_testNow.AddMinutes(60));

        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoBlacklistAsync_WhenAlreadyBlacklisted_DoesNotAddDuplicate()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Previous block",
            _testNow.AddHours(1));
        _restrictions.Add(existingRestriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        await service.AutoBlacklistAsync("192.168.1.100", "Rate limit exceeded", 60);

        // Assert
        _restrictions.Should().HaveCount(1); // No new entry added
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoBlacklistAsync_InvalidatesCache()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.AutoBlacklistAsync("192.168.1.100", "Rate limit exceeded", 60);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync("ip:whitelist", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("ip:blacklist", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoBlacklistAsync_WhenExistingBlacklistExpired_AddsNewEntry()
    {
        // Arrange
        var expiredRestriction = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Expired block",
            _testNow.AddMinutes(-10)); // Expired
        _restrictions.Add(expiredRestriction);
        SetupDbSet(_restrictions);

        var service = CreateService();

        // Act
        await service.AutoBlacklistAsync("192.168.1.100", "New block", 60);

        // Assert
        _restrictions.Should().HaveCount(2); // New entry added
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoBlacklistAsync_SetsCorrectExpirationTime()
    {
        // Arrange
        var service = CreateService();
        var durationMinutes = 120;

        // Act
        await service.AutoBlacklistAsync("192.168.1.100", "Test", durationMinutes);

        // Assert
        _restrictions.Should().HaveCount(1);
        _restrictions[0].ExpiresAt.Should().Be(_testNow.AddMinutes(durationMinutes));
    }

    #endregion

    #region InvalidateCacheAsync Tests

    [Fact]
    public async Task InvalidateCacheAsync_RemovesBothCacheKeys()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InvalidateCacheAsync();

        // Assert
        _mockCache.Verify(x => x.RemoveAsync("ip:whitelist", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync("ip:blacklist", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

// Async query provider for EF Core mocking
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, new object[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
