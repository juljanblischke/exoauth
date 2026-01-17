using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Commands.PasskeyRegisterOptions;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using Fido2NetLib;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class PasskeyRegisterOptionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IPasskeyService> _mockPasskeyService;

    public PasskeyRegisterOptionsHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockPasskeyService = new Mock<IPasskeyService>();
    }

    private PasskeyRegisterOptionsHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockPasskeyService.Object);

    [Fact]
    public async Task Handle_WithValidUser_ReturnsOptions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PasskeyRegisterOptionsCommand();
        var user = CreateActiveUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var expectedOptions = new CredentialCreateOptions();
        var expectedChallengeId = "challenge123";
        _mockPasskeyService.Setup(x => x.CreateRegistrationOptionsAsync(
                user,
                It.IsAny<IEnumerable<byte[]>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOptions, expectedChallengeId));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Options.Should().Be(expectedOptions);
        result.ChallengeId.Should().Be(expectedChallengeId);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new PasskeyRegisterOptionsCommand();
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
        var command = new PasskeyRegisterOptionsCommand();
        var users = new List<SystemUser>();

        SetupMockDbSets(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUserInactiveException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PasskeyRegisterOptionsCommand();
        var user = CreateInactiveUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UserInactiveException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserLocked_ThrowsAccountLockedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PasskeyRegisterOptionsCommand();
        var user = CreateLockedUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_PassesExistingCredentialIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PasskeyRegisterOptionsCommand();
        var user = CreateActiveUser(userId);

        var existingPasskey = CreatePasskey(userId, "My Passkey");
        AddPasskeyToUser(user, existingPasskey);

        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        IEnumerable<byte[]>? capturedCredentialIds = null;
        _mockPasskeyService.Setup(x => x.CreateRegistrationOptionsAsync(
                user,
                It.IsAny<IEnumerable<byte[]>>(),
                It.IsAny<CancellationToken>()))
            .Callback<SystemUser, IEnumerable<byte[]>, CancellationToken>((_, ids, _) => capturedCredentialIds = ids)
            .ReturnsAsync((new CredentialCreateOptions(), "challenge123"));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCredentialIds.Should().NotBeNull();
        capturedCredentialIds.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(existingPasskey.CredentialId);
    }

    private void SetupMockDbSets(List<SystemUser> users)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
    }

    private static SystemUser CreateActiveUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static SystemUser CreateInactiveUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.Deactivate();
        return user;
    }

    private static SystemUser CreateLockedUser(Guid userId)
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.Lock(DateTime.UtcNow.AddHours(1)); // Lock for 1 hour
        return user;
    }

    private static Passkey CreatePasskey(Guid userId, string name)
    {
        return Passkey.Create(
            userId: userId,
            credentialId: new byte[] { 1, 2, 3 },
            publicKey: new byte[] { 4, 5, 6 },
            counter: 0,
            credType: "public-key",
            aaGuid: Guid.NewGuid(),
            name: name);
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
