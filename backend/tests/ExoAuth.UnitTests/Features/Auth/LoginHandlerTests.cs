using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Commands.Login;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class LoginHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IBruteForceProtectionService> _mockBruteForceService;
    private readonly Mock<IPermissionCacheService> _mockPermissionCache;
    private readonly Mock<IForceReauthService> _mockForceReauthService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<IDeviceService> _mockDeviceService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IMfaService> _mockMfaService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRiskScoringService> _mockRiskScoringService;
    private readonly Mock<ILoginPatternService> _mockLoginPatternService;
    private readonly Mock<IGeoIpService> _mockGeoIpService;
    private readonly Mock<IDeviceDetectionService> _mockDeviceDetectionService;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockBruteForceService = new Mock<IBruteForceProtectionService>();
        _mockPermissionCache = new Mock<IPermissionCacheService>();
        _mockForceReauthService = new Mock<IForceReauthService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockDeviceService = new Mock<IDeviceService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockMfaService = new Mock<IMfaService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockRiskScoringService = new Mock<IRiskScoringService>();
        _mockLoginPatternService = new Mock<ILoginPatternService>();
        _mockGeoIpService = new Mock<IGeoIpService>();
        _mockDeviceDetectionService = new Mock<IDeviceDetectionService>();

        // Default token service setup
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(30));

        // Default configuration setup
        _mockConfiguration.Setup(x => x["SystemInvite:BaseUrl"]).Returns("http://localhost:5173");

        // Default email template service setup
        _mockEmailTemplateService.Setup(x => x.GetSubject(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string template, string lang) => $"Subject for {template}");

        // Default device service setup - return a trusted device by default (allows login without approval)
        var mockDevice = TestDataFactory.CreateDevice(Guid.NewGuid());
        _mockDeviceService.Setup(x => x.GenerateDeviceId()).Returns("test-device-id");
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDevice);

        // Default risk scoring setup - low risk, no approval required
        _mockGeoIpService.Setup(x => x.GetLocation(It.IsAny<string?>()))
            .Returns(GeoLocation.Empty);
        _mockDeviceDetectionService.Setup(x => x.Parse(It.IsAny<string?>()))
            .Returns(DeviceInfo.Empty);
        _mockRiskScoringService.Setup(x => x.CalculateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RiskScore.Low());
        _mockRiskScoringService.Setup(x => x.RequiresApproval(It.IsAny<RiskScore>()))
            .Returns(false);
        _mockRiskScoringService.Setup(x => x.CheckForSpoofingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Device>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpoofingCheckResult.NotSuspicious());

        _handler = new LoginHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            _mockBruteForceService.Object,
            _mockPermissionCache.Object,
            _mockForceReauthService.Object,
            _mockRevokedSessionService.Object,
            _mockDeviceService.Object,
            _mockAuditService.Object,
            _mockMfaService.Object,
            _mockEmailService.Object,
            _mockEmailTemplateService.Object,
            _mockConfiguration.Object,
            _mockRiskScoringService.Object,
            _mockLoginPatternService.Object,
            _mockGeoIpService.Object,
            _mockDeviceDetectionService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        // Use non-system permissions to avoid MFA requirement
        var permissions = new List<string> { "clients:read", "clients:create" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid?>()))
            .Returns("access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("test@example.com");
        result.User.Permissions.Should().BeEquivalentTo(permissions);

        _mockBruteForceService.Verify(x => x.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserLogin,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBlocked_ThrowsAccountLockedException()
    {
        // Arrange
        var command = new LoginCommand("blocked@example.com", "Password123!");
        var lockedUntil = DateTime.UtcNow.AddMinutes(15);

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockBruteForceService.Setup(x => x.GetLockoutStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(5, true, 900, lockedUntil, false));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountLockedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.LockedUntil.Should().Be(lockedUntil);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.LoginBlocked,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("notfound@example.com", "Password123!");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(1, false, 0, null, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockBruteForceService.Verify(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(1, false, 0, null, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        _mockBruteForceService.Verify(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsUserInactiveException()
    {
        // Arrange
        var command = new LoginCommand("inactive@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "inactive@example.com", passwordHash: "hashed", isActive: true);
        user.Deactivate(); // Make user inactive

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<UserInactiveException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithFailedAttempts_RecordsFailedAttemptAndLocksAfterThreshold()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var lockedUntil = DateTime.UtcNow.AddMinutes(15);

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        // 7th attempt triggers 15 min lockout (progressive delays: [0, 0, 60, 120, 300, 600, 900, ...])
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(7, true, 900, lockedUntil, true));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountLockedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.LockedUntil.Should().Be(lockedUntil);

        // Should log failed attempt, account locked, and send email
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserLoginFailed,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.AccountLocked,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Email should be sent since ShouldNotify is true
        _mockEmailService.Verify(x => x.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "account-locked",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithProgressiveLockout_DoesNotSendEmailForShortLockout()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        var lockedUntil = DateTime.UtcNow.AddSeconds(60);

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        // 3rd attempt triggers 60s lockout - too short for notification
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(3, true, 60, lockedUntil, false)); // ShouldNotify = false

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Email should NOT be sent since ShouldNotify is false
        _mockEmailService.Verify(x => x.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithLockedUserInDatabase_ThrowsAccountLockedException()
    {
        // Arrange
        var command = new LoginCommand("locked@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "locked@example.com", passwordHash: "hashed");
        var lockedUntil = DateTime.UtcNow.AddHours(1);
        user.Lock(lockedUntil); // Lock user in database

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Not blocked in Redis
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountLockedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        exception.LockedUntil.Should().Be(lockedUntil);

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.LoginBlocked,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SavesRefreshTokenToDatabase()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        // Use non-system permissions to avoid MFA requirement
        var permissions = new List<string> { "clients:read" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid?>()))
            .Returns("access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.RefreshTokens.AddAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var command = new LoginCommand("TEST@EXAMPLE.COM", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser?)null);
        _mockBruteForceService.Setup(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockoutResult(1, false, 0, null, false));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Verify email was normalized to lowercase
        _mockUserRepository.Verify(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMfaEnabled_ReturnsMfaRequiredResponse()
    {
        // Arrange
        var command = new LoginCommand("mfa@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "mfa@example.com", passwordHash: "hashed");
        user.EnableMfa(); // Enable MFA on the user
        var permissions = new List<string> { "system:users:read" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockMfaService.Setup(x => x.GenerateMfaToken(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns("mfa-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MfaRequired.Should().BeTrue();
        result.MfaToken.Should().Be("mfa-token");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenSystemPermissionsWithoutMfa_ReturnsMfaSetupRequiredResponse()
    {
        // Arrange
        var command = new LoginCommand("admin@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "admin@example.com", passwordHash: "hashed");
        // User has system permissions but MFA is NOT enabled
        var permissions = new List<string> { "system:users:read", "system:users:create" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockMfaService.Setup(x => x.GenerateMfaToken(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns("setup-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MfaSetupRequired.Should().BeTrue();
        result.SetupToken.Should().Be("setup-token");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ResetsFailedAttemptsOnSuccessfulLogin()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = TestDataFactory.CreateSystemUser(email: "test@example.com", passwordHash: "hashed");
        // Simulate user had some failed attempts before
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        var permissions = new List<string> { "clients:read" };

        _mockBruteForceService.Setup(x => x.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<UserType>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Guid?>()))
            .Returns("access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - both Redis and database counters should be reset
        _mockBruteForceService.Verify(x => x.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
