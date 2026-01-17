using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Commands.PasskeyLogin;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Passkeys;

public sealed class PasskeyLoginHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IPasskeyService> _mockPasskeyService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IForceReauthService> _mockForceReauthService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IDeviceService> _mockDeviceService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILoginPatternService> _mockLoginPatternService;
    private readonly Mock<IGeoIpService> _mockGeoIpService;
    private readonly Mock<IDeviceDetectionService> _mockDeviceDetectionService;

    public PasskeyLoginHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockPasskeyService = new Mock<IPasskeyService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockForceReauthService = new Mock<IForceReauthService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockDeviceService = new Mock<IDeviceService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLoginPatternService = new Mock<ILoginPatternService>();
        _mockGeoIpService = new Mock<IGeoIpService>();
        _mockDeviceDetectionService = new Mock<IDeviceDetectionService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockGeoIpService.Setup(x => x.GetLocation(It.IsAny<string>()))
            .Returns(new GeoLocation("127.0.0.1", "United States", "US", "New York", null, null));
        _mockDeviceDetectionService.Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new DeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop"));
        _mockTokenService.Setup(x => x.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<UserType>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns("access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(7));
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
            It.IsAny<Guid>(), It.IsAny<Func<Task<List<string>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "permission1" });
    }

    private PasskeyLoginHandler CreateHandler() => new(
        _mockContext.Object,
        _mockPasskeyService.Object,
        _mockTokenService.Object,
        _mockPermissionCache.Object,
        _mockUserRepository.Object,
        _mockForceReauthService.Object,
        _mockRevokedSessionService.Object,
        _mockDeviceService.Object,
        _mockAuditService.Object,
        _mockLoginPatternService.Object,
        _mockGeoIpService.Object,
        _mockDeviceDetectionService.Object);

    [Fact]
    public async Task Handle_WithValidPasskey_LogsInSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };
        var deviceId = Guid.NewGuid();

        var user = CreateActiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };
        var refreshTokens = new List<Domain.Entities.RefreshToken>();

        SetupMockDbSets(passkeys, refreshTokens);

        var command = CreateCommand(credentialId);

        _mockPasskeyService.Setup(x => x.VerifyLoginAsync(
            It.IsAny<string>(), It.IsAny<AuthenticatorAssertionRawResponse>(),
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint)1);

        var device = CreateDevice(deviceId, userId);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
            userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Id.Should().Be(userId);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PasskeyLogin,
            userId,
            null,
            "Passkey",
            passkeyId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasskeyNotFound_ThrowsPasskeyInvalidCredentialException()
    {
        // Arrange
        var credentialId = new byte[] { 1, 2, 3 };
        var passkeys = new List<Passkey>(); // Empty - no passkeys

        SetupMockDbSets(passkeys, new List<Domain.Entities.RefreshToken>());

        var command = CreateCommand(credentialId);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyInvalidCredentialException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUserInactiveException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };

        var user = CreateInactiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(passkeys, new List<Domain.Entities.RefreshToken>());

        var command = CreateCommand(credentialId);
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
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };

        var user = CreateLockedUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(passkeys, new List<Domain.Entities.RefreshToken>());

        var command = CreateCommand(credentialId);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenVerificationFails_ThrowsPasskeyInvalidCredentialException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };

        var user = CreateActiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };

        SetupMockDbSets(passkeys, new List<Domain.Entities.RefreshToken>());

        var command = CreateCommand(credentialId);

        _mockPasskeyService.Setup(x => x.VerifyLoginAsync(
            It.IsAny<string>(), It.IsAny<AuthenticatorAssertionRawResponse>(),
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<PasskeyInvalidCredentialException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenNewDevice_CreatesDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };
        var newDeviceId = Guid.NewGuid();

        var user = CreateActiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };
        var refreshTokens = new List<Domain.Entities.RefreshToken>();

        SetupMockDbSets(passkeys, refreshTokens);

        var command = CreateCommand(credentialId);

        _mockPasskeyService.Setup(x => x.VerifyLoginAsync(
            It.IsAny<string>(), It.IsAny<AuthenticatorAssertionRawResponse>(),
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint)1);

        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
            userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var newDevice = CreateDevice(newDeviceId, userId);
        _mockDeviceService.Setup(x => x.GenerateDeviceId()).Returns(newDeviceId.ToString());
        _mockDeviceService.Setup(x => x.CreateTrustedDeviceAsync(
            userId, newDeviceId.ToString(), It.IsAny<DeviceInfo>(), It.IsAny<GeoLocation>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newDevice);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockDeviceService.Verify(x => x.CreateTrustedDeviceAsync(
            userId, newDeviceId.ToString(), It.IsAny<DeviceInfo>(), It.IsAny<GeoLocation>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingDevice_RecordsUsage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };
        var deviceId = Guid.NewGuid();

        var user = CreateActiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };
        var refreshTokens = new List<Domain.Entities.RefreshToken>();

        SetupMockDbSets(passkeys, refreshTokens);

        var command = CreateCommand(credentialId);

        _mockPasskeyService.Setup(x => x.VerifyLoginAsync(
            It.IsAny<string>(), It.IsAny<AuthenticatorAssertionRawResponse>(),
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((uint)1);

        var device = CreateDevice(deviceId, userId);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
            userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDeviceService.Verify(x => x.RecordUsageAsync(
            deviceId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesPasskeyCounter()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passkeyId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };
        var deviceId = Guid.NewGuid();
        var newCounter = 5u;

        var user = CreateActiveUser(userId);
        var passkey = CreatePasskeyWithUser(passkeyId, userId, credentialId, user);
        var passkeys = new List<Passkey> { passkey };
        var refreshTokens = new List<Domain.Entities.RefreshToken>();

        SetupMockDbSets(passkeys, refreshTokens);

        var command = CreateCommand(credentialId);

        _mockPasskeyService.Setup(x => x.VerifyLoginAsync(
            It.IsAny<string>(), It.IsAny<AuthenticatorAssertionRawResponse>(),
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCounter);

        var device = CreateDevice(deviceId, userId);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
            userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        passkey.Counter.Should().Be(newCounter);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    private static PasskeyLoginCommand CreateCommand(byte[] credentialId)
    {
        var assertionResponse = new AuthenticatorAssertionRawResponse
        {
            Id = credentialId,
            RawId = credentialId,
            Response = new AuthenticatorAssertionRawResponse.AssertionResponse
            {
                AuthenticatorData = new byte[] { 1 },
                ClientDataJson = new byte[] { 2 },
                Signature = new byte[] { 3 }
            },
            Type = PublicKeyCredentialType.PublicKey
        };

        return new PasskeyLoginCommand(
            ChallengeId: "challenge123",
            AssertionResponse: assertionResponse,
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0",
            DeviceFingerprint: "fingerprint",
            DeviceId: null,
            RememberMe: false);
    }

    private void SetupMockDbSets(List<Passkey> passkeys, List<Domain.Entities.RefreshToken> refreshTokens)
    {
        var mockPasskeysDbSet = CreateAsyncMockDbSet(passkeys);
        var mockRefreshTokensDbSet = CreateAsyncMockDbSet(refreshTokens);

        _mockContext.Setup(x => x.Passkeys).Returns(mockPasskeysDbSet.Object);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokensDbSet.Object);
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

    private static Passkey CreatePasskeyWithUser(Guid passkeyId, Guid userId, byte[] credentialId, SystemUser user)
    {
        var passkey = Passkey.Create(userId, credentialId, new byte[] { 4, 5, 6 }, 0, "public-key", Guid.NewGuid(), "My Passkey");
        SetId(passkey, passkeyId);
        SetPasskeyUser(passkey, user);
        return passkey;
    }

    private static Device CreateDevice(Guid deviceId, Guid userId)
    {
        var device = Device.CreateTrusted(userId, deviceId.ToString(), null, "Test Device", "Mozilla/5.0", "127.0.0.1");
        SetId(device, deviceId);
        return device;
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

    private static void SetPasskeyUser(Passkey passkey, SystemUser user)
    {
        var userField = typeof(Passkey)
            .GetProperty("User", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?
            .GetSetMethod(true);
        userField?.Invoke(passkey, new object?[] { user });
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

        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .ReturnsAsync((T entity, CancellationToken _) => null!);

        return mockSet;
    }
}
