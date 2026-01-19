using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Application.Features.Auth.Commands.LoginWithMagicLink;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.MagicLink;

public sealed class LoginWithMagicLinkHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IMagicLinkService> _mockMagicLinkService;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
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
    private readonly Mock<ILogger<LoginWithMagicLinkHandler>> _mockLogger;
    private readonly LoginWithMagicLinkHandler _handler;

    public LoginWithMagicLinkHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockMagicLinkService = new Mock<IMagicLinkService>();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
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
        _mockLogger = new Mock<ILogger<LoginWithMagicLinkHandler>>();

        // Default token service setup
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(30));
        _mockTokenService.Setup(x => x.RememberMeExpirationDays).Returns(90);
        _mockTokenService.Setup(x => x.GenerateAccessToken(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<UserType>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Guid>()))
            .Returns("test-access-token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("test-refresh-token");

        // Default configuration setup
        _mockConfiguration.Setup(x => x["SystemInvite:BaseUrl"]).Returns("http://localhost:5173");

        // Default email template service setup
        _mockEmailTemplateService.Setup(x => x.GetSubject(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string template, string lang) => $"Subject for {template}");

        // Default device service setup - return a trusted device by default
        var mockDevice = TestDataFactory.CreateDevice(Guid.NewGuid());
        _mockDeviceService.Setup(x => x.GenerateDeviceId()).Returns("test-device-id");
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDevice);

        // Default geo IP and device detection setup
        _mockGeoIpService.Setup(x => x.GetLocation(It.IsAny<string?>()))
            .Returns(GeoLocation.Empty);
        _mockDeviceDetectionService.Setup(x => x.Parse(It.IsAny<string?>()))
            .Returns(DeviceInfo.Empty);

        // Default risk scoring setup - not suspicious
        _mockRiskScoringService.Setup(x => x.CalculateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RiskScore.Low());
        _mockRiskScoringService.Setup(x => x.CheckForSpoofingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Device>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpoofingCheckResult.NotSuspicious());

        // Default permission cache setup
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Func<Task<List<string>>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "basic:read" });

        _handler = new LoginWithMagicLinkHandler(
            _mockContext.Object,
            _mockMagicLinkService.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
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
            _mockDeviceDetectionService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsAuthResponse()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(user.Email);
        _mockMagicLinkService.Verify(x => x.MarkAsUsedAsync(magicLinkToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsMagicLinkTokenInvalidException()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("invalid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MagicLinkToken?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None).AsTask())
            .Should().ThrowAsync<MagicLinkTokenInvalidException>();

        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.MagicLinkLoginFailed,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsMagicLinkTokenInvalidException()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        user.Deactivate();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None).AsTask())
            .Should().ThrowAsync<MagicLinkTokenInvalidException>();
    }

    [Fact]
    public async Task Handle_WithLockedUser_ThrowsMagicLinkTokenInvalidException()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        user.Lock(DateTime.UtcNow.AddHours(1)); // Lock for 1 hour
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None).AsTask())
            .Should().ThrowAsync<MagicLinkTokenInvalidException>();
    }

    [Fact]
    public async Task Handle_WithMfaEnabled_ReturnsMfaRequired()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        user.EnableMfa();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockMfaService.Setup(x => x.GenerateMfaToken(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns("mfa-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.MfaRequired.Should().BeTrue();
        result.MfaToken.Should().Be("mfa-token");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithSystemPermissionsAndNoMfa_ReturnsMfaSetupRequired()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionCache.Setup(x => x.GetOrSetPermissionsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Func<Task<List<string>>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "system:users:read" });
        _mockMfaService.Setup(x => x.GenerateMfaToken(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns("setup-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.MfaSetupRequired.Should().BeTrue();
        result.SetupToken.Should().Be("setup-token");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNewDevice_ReturnsDeviceApprovalRequired()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var pendingDevice = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null); // No trusted device found
        _mockDeviceService.Setup(x => x.CreatePendingDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PendingDeviceResult(pendingDevice, "approval-token", "ABCD-1234"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.DeviceApprovalRequired.Should().BeTrue();
        result.ApprovalToken.Should().Be("approval-token");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithSuspiciousTrustedDevice_ReturnsDeviceApprovalRequired()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var pendingDevice = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _mockRiskScoringService.Setup(x => x.CheckForSpoofingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Device>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpoofingCheckResult.Suspicious(85, new[] { "Device spoofing detected" }));
        _mockDeviceService.Setup(x => x.CreatePendingDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<DeviceInfo>(),
                It.IsAny<GeoLocation>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PendingDeviceResult(pendingDevice, "approval-token", "ABCD-1234"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.DeviceApprovalRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarksTokenAsUsed()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMagicLinkService.Verify(x => x.MarkAsUsedAsync(magicLinkToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RecordsLoginPattern()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token", IpAddress: "192.168.1.1");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLoginPatternService.Verify(x => x.RecordLoginAsync(
            user.Id,
            It.IsAny<GeoLocation>(),
            It.IsAny<string?>(),
            "192.168.1.1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LogsSuccessfulLogin()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.MagicLinkLogin,
            user.Id,
            It.IsAny<Guid?>(),
            "SystemUser",
            user.Id,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesUserLastLogin()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser();
        var magicLinkToken = TestDataFactory.CreateMagicLinkToken(user.Id);
        magicLinkToken.User = user;
        var device = TestDataFactory.CreateDevice(user.Id);
        var command = new LoginWithMagicLinkCommand("valid-token");

        _mockMagicLinkService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(magicLinkToken);
        _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockDeviceService.Setup(x => x.FindTrustedDeviceAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.UpdateAsync(
            It.Is<SystemUser>(u => u.LastLoginAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
