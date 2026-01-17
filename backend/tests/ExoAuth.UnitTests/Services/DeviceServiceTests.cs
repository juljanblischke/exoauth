using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.Infrastructure.Services;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class DeviceServiceTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IGeoIpService> _mockGeoIpService;
    private readonly Mock<IDeviceDetectionService> _mockDeviceDetectionService;
    private readonly Mock<IRevokedSessionService> _mockRevokedSessionService;
    private readonly Mock<ILogger<DeviceService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly List<Device> _devices;
    private readonly List<RefreshToken> _refreshTokens;

    public DeviceServiceTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockGeoIpService = new Mock<IGeoIpService>();
        _mockDeviceDetectionService = new Mock<IDeviceDetectionService>();
        _mockRevokedSessionService = new Mock<IRevokedSessionService>();
        _mockLogger = new Mock<ILogger<DeviceService>>();

        var configValues = new Dictionary<string, string?>
        {
            { "DeviceTrust:ApprovalExpiryMinutes", "30" },
            { "DeviceTrust:MaxCodeAttempts", "3" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _devices = new List<Device>();
        _refreshTokens = new List<RefreshToken>();

        SetupMockDbSets();
    }

    private DeviceService CreateService() => new(
        _mockContext.Object,
        _mockGeoIpService.Object,
        _mockDeviceDetectionService.Object,
        _mockRevokedSessionService.Object,
        _configuration,
        _mockLogger.Object);

    // ============ Device Lookup Tests ============

    [Fact]
    public async Task FindTrustedDeviceAsync_WithMatchingDeviceId_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device = CreateTrustedDeviceForTest(userId, "device-123");
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.FindTrustedDeviceAsync(userId, "device-123");

        // Assert
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be("device-123");
    }

    [Fact]
    public async Task FindTrustedDeviceAsync_WithMatchingFingerprint_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device = CreateTrustedDeviceForTest(userId, "device-123", fingerprint: "fp-abc");
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act  - search with different deviceId but matching fingerprint
        var result = await service.FindTrustedDeviceAsync(userId, "different-device-id", "fp-abc");

        // Assert
        result.Should().NotBeNull();
        result!.Fingerprint.Should().Be("fp-abc");
    }

    [Fact]
    public async Task FindTrustedDeviceAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.FindTrustedDeviceAsync(userId, "nonexistent-device");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindTrustedDeviceAsync_OnlyFindsTrustedDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pendingDevice = CreatePendingDeviceForTest(userId, "device-123");
        _devices.Add(pendingDevice);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.FindTrustedDeviceAsync(userId, "device-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingDevice_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(deviceId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(deviceId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingDevice_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllForUserAsync_ReturnsActiveDevicesOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device1 = CreateTrustedDeviceForTest(userId, "device-1");
        var device2 = CreatePendingDeviceForTest(userId, "device-2");
        var device3 = CreateRevokedDeviceForTest(userId, "device-3");
        _devices.AddRange([device1, device2, device3]);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.GetAllForUserAsync(userId);

        // Assert
        result.Should().HaveCount(2); // Trusted and Pending, not Revoked
        result.Should().Contain(d => d.DeviceId == "device-1");
        result.Should().Contain(d => d.DeviceId == "device-2");
    }

    [Fact]
    public async Task GetTrustedDevicesAsync_ReturnsTrustedOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device1 = CreateTrustedDeviceForTest(userId, "device-1");
        var device2 = CreatePendingDeviceForTest(userId, "device-2");
        _devices.AddRange([device1, device2]);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.GetTrustedDevicesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].DeviceId.Should().Be("device-1");
    }

    [Fact]
    public async Task GetPendingDevicesAsync_ReturnsPendingOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device1 = CreateTrustedDeviceForTest(userId, "device-1");
        var device2 = CreatePendingDeviceForTest(userId, "device-2");
        _devices.AddRange([device1, device2]);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.GetPendingDevicesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].DeviceId.Should().Be("device-2");
    }

    [Fact]
    public async Task HasAnyTrustedDeviceAsync_WhenHasTrustedDevices_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device = CreateTrustedDeviceForTest(userId, "device-1");
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.HasAnyTrustedDeviceAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyTrustedDeviceAsync_WhenNoTrustedDevices_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.HasAnyTrustedDeviceAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    // ============ Device Creation Tests ============

    [Fact]
    public async Task CreateTrustedDeviceAsync_CreatesAndSavesDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceInfo = new DeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        var geoLocation = new GeoLocation("127.0.0.1", "Germany", "DE", "Berlin", 52.52, 13.405);

        var service = CreateService();

        // Act
        var result = await service.CreateTrustedDeviceAsync(
            userId, "new-device-id", deviceInfo, geoLocation, "fingerprint-123");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.DeviceId.Should().Be("new-device-id");
        result.Status.Should().Be(DeviceStatus.Trusted);
        result.Browser.Should().Be("Chrome");
        result.Country.Should().Be("Germany");

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTrustedDeviceAsync_WhenDeviceExists_UpdatesUsage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingDevice = CreateTrustedDeviceForTest(userId, "existing-device-id", fingerprint: "fp-123");
        _devices.Add(existingDevice);
        SetupMockDbSets();

        var deviceInfo = new DeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        var geoLocation = new GeoLocation("192.168.1.1", "USA", "US", "New York", 40.7, -74.0);

        var service = CreateService();

        // Act
        var result = await service.CreateTrustedDeviceAsync(
            userId, "existing-device-id", deviceInfo, geoLocation);

        // Assert
        result.Should().Be(existingDevice);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePendingDeviceAsync_CreatesDeviceWithApprovalData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceInfo = new DeviceInfo("Firefox", "119.0", "Linux", "Ubuntu", "Desktop");
        var geoLocation = new GeoLocation("10.0.0.1", "France", "FR", "Paris", 48.86, 2.35);
        var riskFactors = new List<string> { "new_device", "new_location" };

        var service = CreateService();

        // Act
        var result = await service.CreatePendingDeviceAsync(
            userId, "pending-device-id", 75, riskFactors, deviceInfo, geoLocation);

        // Assert
        result.Should().NotBeNull();
        result.Device.UserId.Should().Be(userId);
        result.Device.Status.Should().Be(DeviceStatus.PendingApproval);
        result.Device.RiskScore.Should().Be(75);
        result.ApprovalToken.Should().NotBeNullOrEmpty();
        result.ApprovalCode.Should().NotBeNullOrEmpty();
        result.ApprovalCode.Should().HaveLength(9); // Format: XXXX-XXXX

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePendingDeviceAsync_ReusesExistingDeviceForSameDeviceId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingPending = CreatePendingDeviceForTest(userId, "device-id");
        var originalId = existingPending.Id;
        _devices.Add(existingPending);
        SetupMockDbSets();

        var deviceInfo = new DeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        var geoLocation = new GeoLocation("127.0.0.1", "Germany", "DE", "Berlin", 52.52, 13.405);

        var service = CreateService();

        // Act
        var result = await service.CreatePendingDeviceAsync(
            userId, "device-id", 75, new[] { "new_factor" }, deviceInfo, geoLocation);

        // Assert - existing device should be reused and reset to pending
        result.Device.Id.Should().Be(originalId); // Same device record
        result.Device.Status.Should().Be(DeviceStatus.PendingApproval);
        result.Device.RiskScore.Should().Be(75); // Updated risk score
        result.ApprovalToken.Should().NotBeNullOrEmpty();
        result.ApprovalCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePendingDeviceAsync_ResetsTrustedDeviceToPending_ForSpoofingScenario()
    {
        // Arrange - Trusted device exists (spoofing detection should reset it to pending)
        var userId = Guid.NewGuid();
        var trustedDevice = Device.CreateTrusted(userId, "device-id", null, "My Device", null, "192.168.1.1");
        var originalId = trustedDevice.Id;
        _devices.Add(trustedDevice);
        SetupMockDbSets();

        var deviceInfo = new DeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        var geoLocation = new GeoLocation("10.0.0.1", "Germany", "DE", "Berlin", 52.52, 13.405);

        var service = CreateService();

        // Act - Create pending device for same deviceId (simulates spoofing detection)
        var result = await service.CreatePendingDeviceAsync(
            userId, "device-id", 90, new[] { "location_change", "impossible_travel" }, deviceInfo, geoLocation);

        // Assert - existing trusted device should be reset to pending
        result.Device.Id.Should().Be(originalId); // Same device record reused
        result.Device.Status.Should().Be(DeviceStatus.PendingApproval);
        result.Device.TrustedAt.Should().BeNull(); // Trust cleared
        result.Device.RiskScore.Should().Be(90);
        result.ApprovalToken.Should().NotBeNullOrEmpty();
        result.ApprovalCode.Should().NotBeNullOrEmpty();
    }

    // ============ Device Approval Tests ============

    [Fact]
    public async Task ValidateApprovalTokenAsync_WithValidToken_ReturnsDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(userId, "device-id", token, code, 50, "[]", null, null, null, null, 30);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be("device-id");
    }

    [Fact]
    public async Task ValidateApprovalTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalTokenAsync("invalid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApprovalCodeAsync_WithInvalidToken_ReturnsTokenInvalid()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalCodeAsync("invalid-token", "123456");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("APPROVAL_TOKEN_INVALID");
    }

    [Fact]
    public async Task ValidateApprovalCodeAsync_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(userId, "device-id", token, code, 50, "[]", null, null, null, null, 30);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalCodeAsync(token, code);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Device.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApprovalCodeAsync_WithInvalidCode_IncrementsAttempts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(userId, "device-id", token, code, 50, "[]", null, null, null, null, 30);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalCodeAsync(token, "000000");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("APPROVAL_CODE_INVALID");
        result.Attempts.Should().Be(1);
        device.ApprovalAttempts.Should().Be(1);
    }

    [Fact]
    public async Task ValidateApprovalCodeAsync_WhenMaxAttemptsReached_ReturnsMaxAttemptsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(userId, "device-id", token, code, 50, "[]", null, null, null, null, 30);
        // Simulate 2 previous failed attempts
        device.IncrementApprovalAttempts();
        device.IncrementApprovalAttempts();
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ValidateApprovalCodeAsync(token, "000000");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("APPROVAL_MAX_ATTEMPTS");
        result.MaxAttemptsReached.Should().BeTrue();
    }

    [Fact]
    public async Task MarkDeviceTrustedAsync_UpdatesDeviceStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(userId, "device-id", token, code, 50, "[]", null, null, null, null, 30);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        await service.MarkDeviceTrustedAsync(device);

        // Assert
        device.Status.Should().Be(DeviceStatus.Trusted);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveFromSessionAsync_WithValidPendingDevice_ApprovesDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var pendingDevice = CreatePendingDeviceForTest(userId, "device-id");
        TestDataFactory.SetEntityId(pendingDevice, deviceId);
        _devices.Add(pendingDevice);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ApproveFromSessionAsync(deviceId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(DeviceStatus.Trusted);
    }

    [Fact]
    public async Task ApproveFromSessionAsync_WithInvalidDeviceId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.ApproveFromSessionAsync(Guid.NewGuid(), userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveFromSessionAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var pendingDevice = CreatePendingDeviceForTest(userId, "device-id");
        TestDataFactory.SetEntityId(pendingDevice, deviceId);
        _devices.Add(pendingDevice);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.ApproveFromSessionAsync(deviceId, wrongUserId);

        // Assert
        result.Should().BeNull();
    }

    // ============ Device Management Tests ============

    [Fact]
    public async Task RecordUsageAsync_UpdatesDeviceUsageInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        await service.RecordUsageAsync(deviceId, "192.168.1.100", "USA", "Los Angeles");

        // Assert
        device.IpAddress.Should().Be("192.168.1.100");
        device.Country.Should().Be("USA");
        device.City.Should().Be("Los Angeles");
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_WithNonExistentDevice_DoesNothing()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.RecordUsageAsync(Guid.NewGuid(), "192.168.1.100");

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RenameAsync_WithExistingDevice_RenamesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.RenameAsync(deviceId, "My Laptop");

        // Assert
        result.Should().BeTrue();
        device.Name.Should().Be("My Laptop");
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameAsync_WithNonExistentDevice_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RenameAsync(Guid.NewGuid(), "My Laptop");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAsync_WithExistingDevice_RevokesDeviceAndTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        var refreshToken = RefreshToken.Create(userId, UserType.System, "token-123", 30);
        SetRefreshTokenDeviceId(refreshToken, deviceId);
        _devices.Add(device);
        _refreshTokens.Add(refreshToken);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.RevokeAsync(deviceId);

        // Assert
        result.Should().BeTrue();
        device.Status.Should().Be(DeviceStatus.Revoked);
        refreshToken.IsRevoked.Should().BeTrue();
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_WithNonExistentDevice_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RevokeAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllExceptAsync_RevokesAllOtherDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var keepDeviceId = Guid.NewGuid();
        var revokeDeviceId = Guid.NewGuid();

        var keepDevice = TestDataFactory.CreateDeviceWithId(keepDeviceId, userId, "keep-device");
        var revokeDevice = TestDataFactory.CreateDeviceWithId(revokeDeviceId, userId, "revoke-device");
        _devices.AddRange([keepDevice, revokeDevice]);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.RevokeAllExceptAsync(userId, keepDeviceId);

        // Assert
        result.Should().Be(1);
        keepDevice.Status.Should().Be(DeviceStatus.Trusted);
        revokeDevice.Status.Should().Be(DeviceStatus.Revoked);
    }

    [Fact]
    public async Task RevokeAllExceptAsync_WithNoOtherDevices_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = TestDataFactory.CreateDeviceWithId(deviceId, userId);
        _devices.Add(device);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.RevokeAllExceptAsync(userId, deviceId);

        // Assert
        result.Should().Be(0);
        device.Status.Should().Be(DeviceStatus.Trusted);
    }

    [Fact]
    public async Task RemoveAllAsync_RemovesAllUserDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var device1 = CreateTrustedDeviceForTest(userId, "device-1");
        var device2 = CreateTrustedDeviceForTest(userId, "device-2");
        _devices.AddRange([device1, device2]);
        SetupMockDbSets();

        var service = CreateService();

        // Act
        var result = await service.RemoveAllAsync(userId);

        // Assert
        result.Should().Be(2);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAllAsync_WithNoDevices_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.RemoveAllAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    // ============ Utility Tests ============

    [Fact]
    public void GenerateDeviceId_ReturnsUniqueUrlSafeId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var id1 = service.GenerateDeviceId();
        var id2 = service.GenerateDeviceId();

        // Assert
        id1.Should().NotBeNullOrEmpty();
        id2.Should().NotBeNullOrEmpty();
        id1.Should().NotBe(id2);
        id1.Should().NotContain("+");
        id1.Should().NotContain("/");
        id1.Should().NotContain("=");
    }

    // ============ Helper Methods ============

    private void SetupMockDbSets()
    {
        var mockDevicesDbSet = CreateAsyncMockDbSet(_devices);
        _mockContext.Setup(x => x.Devices).Returns(mockDevicesDbSet.Object);

        var mockRefreshTokensDbSet = CreateAsyncMockDbSet(_refreshTokens);
        _mockContext.Setup(x => x.RefreshTokens).Returns(mockRefreshTokensDbSet.Object);

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static Device CreateTrustedDeviceForTest(Guid userId, string deviceId, string? fingerprint = null)
    {
        var device = Device.CreateTrusted(
            userId,
            deviceId,
            fingerprint ?? $"fp-{deviceId}",
            null,
            "Mozilla/5.0",
            "127.0.0.1");
        device.SetDeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        device.SetLocation("Germany", "DE", "Berlin", 52.52, 13.405);
        return device;
    }

    private static Device CreatePendingDeviceForTest(Guid userId, string deviceId)
    {
        var token = Device.GenerateApprovalToken();
        var code = Device.GenerateApprovalCode();
        var device = Device.CreatePending(
            userId, deviceId, token, code, 50, "[]", null, null, "Mozilla/5.0", "127.0.0.1", 30);
        device.SetDeviceInfo("Chrome", "120.0", "Windows", "10", "Desktop");
        device.SetLocation("Germany", "DE", "Berlin", 52.52, 13.405);
        return device;
    }

    private static Device CreateRevokedDeviceForTest(Guid userId, string deviceId)
    {
        var device = CreateTrustedDeviceForTest(userId, deviceId);
        device.Revoke();
        return device;
    }

    private static void SetRefreshTokenDeviceId(RefreshToken token, Guid deviceId)
    {
        var property = typeof(RefreshToken).GetProperty("DeviceId");
        var backingField = typeof(RefreshToken).GetField("<DeviceId>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(token, deviceId);
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

        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>()))
            .Callback<IEnumerable<T>>(entities =>
            {
                foreach (var entity in entities.ToList())
                    data.Remove(entity);
            });

        return mockSet;
    }
}
