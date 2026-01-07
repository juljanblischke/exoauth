using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.IpRestrictions.Commands.CreateIpRestriction;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace ExoAuth.UnitTests.Features.IpRestrictions;

public sealed class CreateIpRestrictionHandlerTests
{
    private readonly Mock<IAppDbContext> _mockDbContext;
    private readonly Mock<IIpRestrictionService> _mockIpRestrictionService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly DateTime _testNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.NewGuid();
    private List<IpRestriction> _restrictions;
    private List<SystemUser> _users;

    public CreateIpRestrictionHandlerTests()
    {
        _mockDbContext = new Mock<IAppDbContext>();
        _mockIpRestrictionService = new Mock<IIpRestrictionService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_testNow);

        _restrictions = new List<IpRestriction>();
        _users = new List<SystemUser>
        {
            CreateSystemUser(_userId, "admin@example.com")
        };

        SetupDbSets();
    }

    private void SetupDbSets()
    {
        var restrictionDbSet = CreateMockDbSet(_restrictions);
        var userDbSet = CreateMockDbSet(_users);
        _mockDbContext.Setup(x => x.IpRestrictions).Returns(restrictionDbSet.Object);
        _mockDbContext.Setup(x => x.SystemUsers).Returns(userDbSet.Object);
    }

    private CreateIpRestrictionHandler CreateHandler()
    {
        return new CreateIpRestrictionHandler(
            _mockDbContext.Object,
            _mockIpRestrictionService.Object,
            _mockDateTimeProvider.Object);
    }

    private static SystemUser CreateSystemUser(Guid id, string email)
    {
        var user = (SystemUser)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SystemUser));
        typeof(SystemUser).GetProperty(nameof(SystemUser.Id))!.SetValue(user, id);
        typeof(SystemUser).GetProperty(nameof(SystemUser.Email))!.SetValue(user, email);
        return user;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidIpAddress_CreatesRestriction()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Suspicious activity",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IpAddress.Should().Be("192.168.1.100");
        result.Type.Should().Be(IpRestrictionType.Blacklist);
        result.Reason.Should().Be("Suspicious activity");
        result.Source.Should().Be(IpRestrictionSource.Manual);
        result.CreatedByUserId.Should().Be(_userId);
        result.CreatedByUserEmail.Should().Be("admin@example.com");

        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCidr_CreatesRestriction()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            "10.0.0.0/8",
            IpRestrictionType.Whitelist,
            "Internal network",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IpAddress.Should().Be("10.0.0.0/8");
        result.Type.Should().Be(IpRestrictionType.Whitelist);
    }

    [Fact]
    public async Task Handle_WithExpiresAt_CreatesRestrictionWithExpiration()
    {
        // Arrange
        var handler = CreateHandler();
        var expiresAt = _testNow.AddDays(7);
        var command = new CreateIpRestrictionCommand(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Temporary block",
            expiresAt,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_InvalidatesCacheAfterCreation()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockIpRestrictionService.Verify(x => x.InvalidateCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    public async Task Handle_WithValidIpv4Addresses_CreatesRestriction(string ipAddress)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            ipAddress,
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IpAddress.Should().Be(ipAddress);
    }

    [Theory]
    [InlineData("10.0.0.0/8")]
    [InlineData("172.16.0.0/12")]
    [InlineData("192.168.0.0/16")]
    [InlineData("192.168.1.0/24")]
    [InlineData("192.168.1.128/25")]
    public async Task Handle_WithValidCidrNotations_CreatesRestriction(string cidr)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            cidr,
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IpAddress.Should().Be(cidr);
    }

    #endregion

    #region Error Cases

    [Theory]
    [InlineData("invalid-ip")]
    [InlineData("192.168.1.256")]
    [InlineData("")]
    [InlineData("abc.def.ghi.jkl")]
    public async Task Handle_WithInvalidIpFormat_ThrowsIpRestrictionInvalidCidrException(string invalidIp)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            invalidIp,
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IpRestrictionInvalidCidrException>()
            .Where(e => e.IpAddress == invalidIp);
    }

    [Theory]
    [InlineData("192.168.1.0/33")]  // IPv4 max is 32
    [InlineData("192.168.1.0/-1")]  // Negative prefix
    [InlineData("192.168.1.0/abc")] // Non-numeric prefix
    [InlineData("192.168.1.0/")]    // Missing prefix
    public async Task Handle_WithInvalidCidrNotation_ThrowsIpRestrictionInvalidCidrException(string invalidCidr)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            invalidCidr,
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IpRestrictionInvalidCidrException>();
    }

    [Fact]
    public async Task Handle_WhenActiveRestrictionExists_ThrowsIpRestrictionAlreadyExistsException()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Existing",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new CreateIpRestrictionCommand(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "New block",
            null,
            _userId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IpRestrictionAlreadyExistsException>()
            .Where(e => e.IpAddress == "192.168.1.100");
    }

    #endregion

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
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));

        return mockSet;
    }
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
