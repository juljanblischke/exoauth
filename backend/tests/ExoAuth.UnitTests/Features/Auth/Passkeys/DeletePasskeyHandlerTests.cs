using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.DeletePasskey;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class DeletePasskeyHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;

    public DeletePasskeyHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockEmailService = new Mock<IEmailService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private DeletePasskeyHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockAuditService.Object,
        _mockEmailService.Object);

    [Fact]
    public async Task Handle_WithValidRequest_DeletesPasskey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var passkeyName = "My Passkey";
        var command = new DeletePasskeyCommand(passkeyId);

        var user = CreateUserWithPassword(userId);
        var passkey = CreatePasskey(passkeyId, userId, passkeyName);
        AddPasskeyToUser(user, passkey);

        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _mockContext.Verify(x => x.Passkeys.Remove(passkey), Times.Once);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasskeyDeleted,
            userId,
            null,
            "Passkey",
            passkeyId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendPasskeyRemovedEmailAsync(
            user.Email,
            user.FullName,
            passkeyName,
            user.Id,
            user.PreferredLanguage,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new DeletePasskeyCommand(Guid.NewGuid());
        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeletePasskeyCommand(Guid.NewGuid());

        var users = new List<SystemUser>(); // Empty
        var passkeys = new List<Passkey>();

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

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
        var command = new DeletePasskeyCommand(Guid.NewGuid());

        var user = CreateUserWithPassword(userId);
        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey>();

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenDeletingLastPasskeyWithoutPassword_ThrowsPasskeyCannotDeleteLastException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var command = new DeletePasskeyCommand(passkeyId);

        var user = CreateUserWithoutPassword(userId);
        var passkey = CreatePasskey(passkeyId, userId, "Only Passkey");
        AddPasskeyToUser(user, passkey);

        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyCannotDeleteLastException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenDeletingLastPasskeyWithPassword_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var command = new DeletePasskeyCommand(passkeyId);

        var user = CreateUserWithPassword(userId);
        var passkey = CreatePasskey(passkeyId, userId, "Only Passkey");
        AddPasskeyToUser(user, passkey);

        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockContext.Verify(x => x.Passkeys.Remove(passkey), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeletingOneOfMultiplePasskeysWithoutPassword_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId1 = Guid.NewGuid();
        var passkeyId2 = Guid.NewGuid();
        var command = new DeletePasskeyCommand(passkeyId1);

        var user = CreateUserWithoutPassword(userId);
        var passkey1 = CreatePasskey(passkeyId1, userId, "Passkey 1");
        var passkey2 = CreatePasskey(passkeyId2, userId, "Passkey 2");
        AddPasskeyToUser(user, passkey1);
        AddPasskeyToUser(user, passkey2);

        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey> { passkey1, passkey2 };

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockContext.Verify(x => x.Passkeys.Remove(passkey1), Times.Once);
    }

    private void SetupMockDbSets(List<SystemUser> users, List<Passkey> passkeys)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockPasskeysDbSet = CreateAsyncMockDbSet(passkeys);

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.Passkeys).Returns(mockPasskeysDbSet.Object);
    }

    private static SystemUser CreateUserWithPassword(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "password_hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static SystemUser CreateUserWithoutPassword(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "ANONYMIZED", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static Passkey CreatePasskey(Guid passkeyId, Guid userId, string name)
    {
        var passkey = Passkey.Create(
            userId: userId,
            credentialId: Guid.NewGuid().ToByteArray(),
            publicKey: new byte[] { 4, 5, 6 },
            counter: 0,
            credType: "public-key",
            aaGuid: Guid.NewGuid(),
            name: name);

        SetId(passkey, passkeyId);
        return passkey;
    }

    private static void AddPasskeyToUser(SystemUser user, Passkey passkey)
    {
        var passkeysField = typeof(SystemUser)
            .GetField("_passkeys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passkeys = (List<Passkey>?)passkeysField?.GetValue(user);
        passkeys?.Add(passkey);
    }

    private static void SetUserId(SystemUser user, Guid userId)
    {
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);
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

        mockSet.Setup(x => x.Remove(It.IsAny<T>())).Callback<T>(entity => data.Remove(entity));

        return mockSet;
    }
}
