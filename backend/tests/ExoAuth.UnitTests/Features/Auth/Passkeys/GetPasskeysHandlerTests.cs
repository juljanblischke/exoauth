using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Queries.GetPasskeys;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class GetPasskeysHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;

    public GetPasskeysHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
    }

    private GetPasskeysHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object);

    [Fact]
    public async Task Handle_WithPasskeys_ReturnsPasskeyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetPasskeysQuery();

        var passkey1 = CreatePasskey(userId, "MacBook Pro");
        var passkey2 = CreatePasskey(userId, "iPhone");
        SetCreatedAt(passkey1, DateTime.UtcNow.AddDays(-1));
        SetCreatedAt(passkey2, DateTime.UtcNow);

        var passkeys = new List<Passkey> { passkey1, passkey2 };

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passkeys.Should().HaveCount(2);
        result.Passkeys.First().Name.Should().Be("iPhone"); // Ordered by CreatedAt desc
        result.Passkeys.Last().Name.Should().Be("MacBook Pro");
    }

    [Fact]
    public async Task Handle_WithNoPasskeys_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetPasskeysQuery();

        var passkeys = new List<Passkey>(); // Empty

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passkeys.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var query = new GetPasskeysQuery();
        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_OnlyReturnsCurrentUserPasskeys()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var query = new GetPasskeysQuery();

        var currentUserPasskey = CreatePasskey(currentUserId, "My Passkey");
        var otherUserPasskey = CreatePasskey(otherUserId, "Other Passkey");

        var passkeys = new List<Passkey> { currentUserPasskey, otherUserPasskey };

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passkeys.Should().HaveCount(1);
        result.Passkeys.First().Name.Should().Be("My Passkey");
    }

    private void SetupMockDbSets(List<Passkey> passkeys)
    {
        var mockPasskeysDbSet = CreateAsyncMockDbSet(passkeys);
        _mockContext.Setup(x => x.Passkeys).Returns(mockPasskeysDbSet.Object);
    }

    private static Passkey CreatePasskey(Guid userId, string name)
    {
        var passkey = Passkey.Create(
            userId: userId,
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: new byte[] { 4, 5, 6 },
            counter: 0,
            credType: "public-key",
            aaGuid: Guid.NewGuid(),
            name: name);

        SetId(passkey, Guid.NewGuid());
        return passkey;
    }

    private static void SetId<T>(T entity, Guid id) where T : class
    {
        var idField = typeof(T).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
    }

    private static void SetCreatedAt<T>(T entity, DateTime createdAt) where T : class
    {
        var field = typeof(T).BaseType?
            .GetField("<CreatedAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(entity, createdAt);
    }

    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsAsyncQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(queryable.Provider);

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

        return mockSet;
    }
}
