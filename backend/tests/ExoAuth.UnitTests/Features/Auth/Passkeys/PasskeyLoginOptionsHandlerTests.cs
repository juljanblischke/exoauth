using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.PasskeyLoginOptions;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using AssertionOptions = Fido2NetLib.AssertionOptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class PasskeyLoginOptionsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IPasskeyService> _mockPasskeyService;

    public PasskeyLoginOptionsHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockPasskeyService = new Mock<IPasskeyService>();
    }

    private PasskeyLoginOptionsHandler CreateHandler() => new(
        _mockContext.Object,
        _mockPasskeyService.Object);

    [Fact]
    public async Task Handle_WithoutEmail_ReturnsOptionsForDiscoverableCredentials()
    {
        // Arrange
        var command = new PasskeyLoginOptionsCommand();

        var expectedOptions = new AssertionOptions();
        var expectedChallengeId = "challenge123";
        _mockPasskeyService.Setup(x => x.CreateLoginOptionsAsync(
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOptions, expectedChallengeId));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Options.Should().Be(expectedOptions);
        result.ChallengeId.Should().Be(expectedChallengeId);

        _mockPasskeyService.Verify(x => x.CreateLoginOptionsAsync(
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmail_ReturnsOptionsWithUserCredentials()
    {
        // Arrange
        var email = "test@example.com";
        var command = new PasskeyLoginOptionsCommand(email);

        var userId = Guid.NewGuid();
        var user = CreateUserWithPasskey(userId, email);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);

        var expectedOptions = new AssertionOptions();
        var expectedChallengeId = "challenge123";
        IEnumerable<byte[]>? capturedCredentialIds = null;

        _mockPasskeyService.Setup(x => x.CreateLoginOptionsAsync(
                It.IsAny<IEnumerable<byte[]>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<byte[]>?, CancellationToken>((ids, _) => capturedCredentialIds = ids)
            .ReturnsAsync((expectedOptions, expectedChallengeId));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedCredentialIds.Should().NotBeNull();
        capturedCredentialIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithEmailNoPasskeys_ReturnsOptionsForDiscoverableCredentials()
    {
        // Arrange
        var email = "test@example.com";
        var command = new PasskeyLoginOptionsCommand(email);

        var userId = Guid.NewGuid();
        var user = CreateActiveUser(userId, email); // No passkeys
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);

        _mockPasskeyService.Setup(x => x.CreateLoginOptionsAsync(
                It.IsAny<IEnumerable<byte[]>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new AssertionOptions(), "challenge123"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsOptionsForDiscoverableCredentials()
    {
        // Arrange
        var email = "unknown@example.com";
        var command = new PasskeyLoginOptionsCommand(email);

        var users = new List<SystemUser>(); // No users

        SetupMockDbSets(users);

        _mockPasskeyService.Setup(x => x.CreateLoginOptionsAsync(
                It.IsAny<IEnumerable<byte[]>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new AssertionOptions(), "challenge123"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithInactiveUser_DoesNotIncludeCredentials()
    {
        // Arrange
        var email = "inactive@example.com";
        var command = new PasskeyLoginOptionsCommand(email);

        var userId = Guid.NewGuid();
        var user = CreateInactiveUserWithPasskey(userId, email);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users);

        IEnumerable<byte[]>? capturedCredentialIds = null;
        _mockPasskeyService.Setup(x => x.CreateLoginOptionsAsync(
                It.IsAny<IEnumerable<byte[]>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<byte[]>?, CancellationToken>((ids, _) => capturedCredentialIds = ids)
            .ReturnsAsync((new AssertionOptions(), "challenge123"));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - inactive user credentials should not be included
        capturedCredentialIds.Should().BeNull();
    }

    private void SetupMockDbSets(List<SystemUser> users)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
    }

    private static SystemUser CreateActiveUser(Guid userId, string email)
    {
        var user = SystemUser.Create(email, "hash", "Test", "User", true);
        SetUserId(user, userId);
        return user;
    }

    private static SystemUser CreateUserWithPasskey(Guid userId, string email)
    {
        var user = SystemUser.Create(email, "hash", "Test", "User", true);
        SetUserId(user, userId);

        var passkey = Passkey.Create(userId, new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, 0, "public-key", Guid.NewGuid(), "Passkey");
        AddPasskeyToUser(user, passkey);

        return user;
    }

    private static SystemUser CreateInactiveUserWithPasskey(Guid userId, string email)
    {
        var user = SystemUser.Create(email, "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.Deactivate();

        var passkey = Passkey.Create(userId, new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, 0, "public-key", Guid.NewGuid(), "Passkey");
        AddPasskeyToUser(user, passkey);

        return user;
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
