using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.RenamePasskey;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class RenamePasskeyHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;

    public RenamePasskeyHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RenamePasskeyHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockAuditService.Object);

    [Fact]
    public async Task Handle_WithValidRequest_RenamesPasskey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var oldName = "Old Name";
        var newName = "New Name";
        var command = new RenamePasskeyCommand(passkeyId, newName);

        var passkey = CreatePasskey(passkeyId, userId, oldName);
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
        passkey.Name.Should().Be(newName);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasskeyRenamed,
            userId,
            null,
            "Passkey",
            passkeyId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RenamePasskeyCommand(Guid.NewGuid(), "New Name");
        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenPasskeyNotFound_ThrowsPasskeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RenamePasskeyCommand(Guid.NewGuid(), "New Name");

        var passkeys = new List<Passkey>(); // Empty

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenPasskeyBelongsToOtherUser_ThrowsPasskeyNotFoundException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var command = new RenamePasskeyCommand(passkeyId, "New Name");

        var passkey = CreatePasskey(passkeyId, otherUserId, "Other User Passkey");
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(currentUserId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    private void SetupMockDbSets(List<Passkey> passkeys)
    {
        var mockPasskeysDbSet = CreateAsyncMockDbSet(passkeys);
        _mockContext.Setup(x => x.Passkeys).Returns(mockPasskeysDbSet.Object);
    }

    private static Passkey CreatePasskey(Guid passkeyId, Guid userId, string name)
    {
        var passkey = Passkey.Create(
            userId: userId,
            credentialId: new byte[] { 1, 2, 3 },
            publicKey: new byte[] { 4, 5, 6 },
            counter: 0,
            credType: "public-key",
            aaGuid: Guid.NewGuid(),
            name: name);

        SetId(passkey, passkeyId);
        return passkey;
    }

    private static void SetId<T>(T entity, Guid id) where T : class
    {
        var idField = typeof(T).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
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
