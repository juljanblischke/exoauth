using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Commands.PasskeyRegister;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class PasskeyRegisterHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IPasskeyService> _mockPasskeyService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEmailService> _mockEmailService;

    public PasskeyRegisterHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockPasskeyService = new Mock<IPasskeyService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockEmailService = new Mock<IEmailService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private PasskeyRegisterHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockPasskeyService.Object,
        _mockAuditService.Object,
        _mockEmailService.Object);

    [Fact]
    public async Task Handle_WithValidCredential_RegistersPasskey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var challengeId = "challenge123";
        var passkeyName = "My MacBook";
        var command = CreateCommand(challengeId, passkeyName);

        var user = CreateActiveUser(userId);
        var users = new List<SystemUser> { user };
        var passkeys = new List<Passkey>();

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var credentialResult = new PasskeyCredentialResult(
            Id: new byte[] { 1, 2, 3 },
            PublicKey: new byte[] { 4, 5, 6 },
            Counter: 0,
            Type: "public-key",
            AaGuid: Guid.NewGuid());

        _mockPasskeyService.Setup(x => x.VerifyRegistrationAsync(
                userId, challengeId, It.IsAny<AuthenticatorAttestationRawResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentialResult);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(passkeyName);
        passkeys.Should().ContainSingle();

        _mockAuditService.Verify(x => x.LogAsync(
            AuditActions.PasskeyRegistered,
            userId,
            null,
            "Passkey",
            It.IsAny<Guid>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendPasskeyRegisteredEmailAsync(
            user.Email,
            user.FullName,
            passkeyName,
            user.PreferredLanguage,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = CreateCommand("challenge", "name");
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
        var command = CreateCommand("challenge", "name");
        var users = new List<SystemUser>();

        SetupMockDbSets(users, new List<Passkey>());
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
        var command = CreateCommand("challenge", "name");
        var user = CreateInactiveUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<Passkey>());
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
        var command = CreateCommand("challenge", "name");
        var user = CreateLockedUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<Passkey>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenVerificationFails_ThrowsPasskeyRegistrationFailedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateCommand("challenge", "name");
        var user = CreateActiveUser(userId);
        var users = new List<SystemUser> { user };

        SetupMockDbSets(users, new List<Passkey>());
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);
        _mockPasskeyService.Setup(x => x.VerifyRegistrationAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AuthenticatorAttestationRawResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasskeyCredentialResult?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyRegistrationFailedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenCredentialAlreadyExists_ThrowsPasskeyAlreadyRegisteredException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingCredentialId = new byte[] { 1, 2, 3 };
        var command = CreateCommand("challenge", "name");

        var user = CreateActiveUser(userId);
        var users = new List<SystemUser> { user };
        var existingPasskey = Passkey.Create(userId, existingCredentialId, new byte[] { 4, 5, 6 }, 0, "public-key", Guid.NewGuid(), "Existing");
        var passkeys = new List<Passkey> { existingPasskey };

        SetupMockDbSets(users, passkeys);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var credentialResult = new PasskeyCredentialResult(
            Id: existingCredentialId,
            PublicKey: new byte[] { 7, 8, 9 },
            Counter: 0,
            Type: "public-key",
            AaGuid: Guid.NewGuid());

        _mockPasskeyService.Setup(x => x.VerifyRegistrationAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AuthenticatorAttestationRawResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentialResult);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyAlreadyRegisteredException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    private static PasskeyRegisterCommand CreateCommand(string challengeId, string name)
    {
        var attestationResponse = new AuthenticatorAttestationRawResponse
        {
            Id = new byte[] { 1, 2, 3 },
            RawId = new byte[] { 1, 2, 3 },
            Response = new AuthenticatorAttestationRawResponse.ResponseData
            {
                AttestationObject = new byte[] { 1 },
                ClientDataJson = new byte[] { 2 }
            },
            Type = PublicKeyCredentialType.PublicKey
        };

        return new PasskeyRegisterCommand(challengeId, attestationResponse, name);
    }

    private void SetupMockDbSets(List<SystemUser> users, List<Passkey> passkeys)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        var mockPasskeysDbSet = CreateAsyncMockDbSet(passkeys);

        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(x => x.Passkeys).Returns(mockPasskeysDbSet.Object);
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

        mockSet.Setup(m => m.Add(It.IsAny<T>()))
            .Callback<T>(entity => data.Add(entity));

        return mockSet;
    }
}
