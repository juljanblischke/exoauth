using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.ResendDeviceApproval;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth.Resend;

public sealed class ResendDeviceApprovalHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly Mock<ILogger<ResendDeviceApprovalHandler>> _mockLogger;
    private readonly ResendDeviceApprovalHandler _handler;
    private readonly List<Device> _devices;
    private readonly List<SystemUser> _users;

    public ResendDeviceApprovalHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCaptchaService = new Mock<ICaptchaService>();
        _mockLogger = new Mock<ILogger<ResendDeviceApprovalHandler>>();
        _devices = new List<Device>();
        _users = new List<SystemUser>();

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);
        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        _handler = new ResendDeviceApprovalHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockCaptchaService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotPendingException_WhenDeviceNotFound()
    {
        // Arrange
        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "invalid-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DeviceApprovalNotPendingException>();
    }

    [Fact]
    public async Task Handle_ThrowsNotPendingException_WhenDeviceNotPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = TestDataFactory.CreateDevice(userId, "device-1");
        TestDataFactory.SetEntityId(device, Guid.NewGuid());
        // Device is trusted, not pending
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "some-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DeviceApprovalNotPendingException>();
    }

    [Fact]
    public async Task Handle_ThrowsNotPendingException_WhenUserInactive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", isActive: false);
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        // Create a pending device
        var device = CreatePendingDevice(userId);
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DeviceApprovalNotPendingException>();
    }

    [Fact]
    public async Task Handle_ValidatesCaptcha_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: "captcha-token",
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCaptchaService.Verify(x => x.ValidateRequiredAsync(
            "captcha-token",
            "device_approval_resend",
            "127.0.0.1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotValidateCaptcha_WhenNotProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCaptchaService.Verify(x => x.ValidateRequiredAsync(
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResendsApprovalEmail_WhenCooldownPassed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("resent");
        result.NewApprovalToken.Should().NotBeNullOrEmpty();
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SendsEmail_WithDeviceInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(x => x.SendDeviceApprovalRequiredAsync(
            "user@example.com",
            "John",
            It.IsAny<string>(), // new approval token
            It.IsAny<string>(), // new approval code
            It.IsAny<string?>(), // device name
            It.IsAny<string?>(), // browser
            It.IsAny<string?>(), // OS
            It.IsAny<string?>(), // location
            It.IsAny<string?>(), // IP
            It.IsAny<int>(), // risk score
            It.IsAny<string?>(), // language
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var deviceId = Guid.NewGuid();
        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetEntityId(device, deviceId);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.DeviceApprovalResent,
            userId,
            It.IsAny<Guid?>(),
            "Device",
            deviceId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsNewApprovalToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateSystemUser("user@example.com", firstName: "John");
        TestDataFactory.SetEntityId(user, userId);
        _users.Add(user);

        var device = CreatePendingDeviceWithUser(userId, user);
        TestDataFactory.SetCreatedAt(device, DateTime.UtcNow.AddMinutes(-5)); // Past cooldown
        _devices.Add(device);

        _mockContext.Setup(x => x.Devices)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_devices).Object);

        var command = new ResendDeviceApprovalCommand(
            ApprovalToken: "test-approval-token",
            CaptchaToken: null,
            IpAddress: "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewApprovalToken.Should().NotBeNullOrEmpty();
        result.NewApprovalToken.Should().NotBe("test-approval-token"); // Should be different from original
    }

    // Helper methods
    private static Device CreatePendingDevice(Guid userId)
    {
        var approvalToken = "test-approval-token";
        var approvalCode = "123456";

        var device = Device.CreatePending(
            userId: userId,
            deviceId: "test-device",
            fingerprint: "test-fingerprint",
            name: "Test Device",
            userAgent: "Mozilla/5.0",
            ipAddress: "127.0.0.1",
            approvalToken: approvalToken,
            approvalCode: approvalCode,
            riskScore: 50,
            riskFactors: "new_location",
            expirationMinutes: 30);

        TestDataFactory.SetEntityId(device, Guid.NewGuid());
        return device;
    }

    private static Device CreatePendingDeviceWithUser(Guid userId, SystemUser user)
    {
        var device = CreatePendingDevice(userId);

        // Use reflection to set the User navigation property
        var userProperty = typeof(Device).GetProperty("User");
        userProperty?.SetValue(device, user);

        return device;
    }
}
