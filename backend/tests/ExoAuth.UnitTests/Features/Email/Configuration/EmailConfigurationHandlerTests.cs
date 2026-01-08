using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Configuration.Commands.UpdateEmailConfiguration;
using ExoAuth.Application.Features.Email.Configuration.Queries.GetEmailConfiguration;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Configuration;

public sealed class GetEmailConfigurationHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetEmailConfigurationHandler _handler;
    private readonly List<EmailConfiguration> _configs;

    public GetEmailConfigurationHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _configs = new List<EmailConfiguration>();

        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        _handler = new GetEmailConfigurationHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_CreatesDefaultConfiguration_WhenNoneExists()
    {
        // Arrange
        var query = new GetEmailConfigurationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MaxRetriesPerProvider.Should().Be(3);
        result.InitialRetryDelayMs.Should().Be(1000);
        result.MaxRetryDelayMs.Should().Be(60000);
        result.BackoffMultiplier.Should().Be(2.0);
        result.CircuitBreakerFailureThreshold.Should().Be(5);
        result.CircuitBreakerWindowMinutes.Should().Be(10);
        result.CircuitBreakerOpenDurationMinutes.Should().Be(30);
        result.AutoRetryDlq.Should().BeFalse();
        result.DlqRetryIntervalHours.Should().Be(6);
        result.EmailsEnabled.Should().BeTrue();
        result.TestMode.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsExistingConfiguration_WhenExists()
    {
        // Arrange
        var config = EmailConfiguration.CreateDefault();
        // Use UpdateAll to set custom values
        config.UpdateAll(
            maxRetriesPerProvider: 5,
            initialRetryDelayMs: 2000,
            maxRetryDelayMs: 120000,
            backoffMultiplier: 3.0,
            circuitBreakerFailureThreshold: 10,
            circuitBreakerWindowMinutes: 15,
            circuitBreakerOpenDurationMinutes: 60,
            autoRetryDlq: true,
            dlqRetryIntervalHours: 12,
            emailsEnabled: false,
            testMode: true);

        _configs.Add(config);
        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        var query = new GetEmailConfigurationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.MaxRetriesPerProvider.Should().Be(5);
        result.InitialRetryDelayMs.Should().Be(2000);
        result.MaxRetryDelayMs.Should().Be(120000);
        result.BackoffMultiplier.Should().Be(3.0);
        result.EmailsEnabled.Should().BeFalse();
        result.TestMode.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SavesDefaultConfiguration_WhenCreated()
    {
        // Arrange
        var query = new GetEmailConfigurationQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class UpdateEmailConfigurationHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly UpdateEmailConfigurationHandler _handler;
    private readonly List<EmailConfiguration> _configs;

    public UpdateEmailConfigurationHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _configs = new List<EmailConfiguration>();

        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        _handler = new UpdateEmailConfigurationHandler(_mockContext.Object, _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_CreatesConfiguration_WhenNoneExists()
    {
        // Arrange
        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 5,
            InitialRetryDelayMs: 2000,
            MaxRetryDelayMs: 120000,
            BackoffMultiplier: 3.0,
            CircuitBreakerFailureThreshold: 10,
            CircuitBreakerWindowMinutes: 15,
            CircuitBreakerOpenDurationMinutes: 60,
            AutoRetryDlq: true,
            DlqRetryIntervalHours: 12,
            EmailsEnabled: true,
            TestMode: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MaxRetriesPerProvider.Should().Be(5);
    }

    [Fact]
    public async Task Handle_UpdatesExistingConfiguration()
    {
        // Arrange
        var existingConfig = EmailConfiguration.CreateDefault();
        TestDataFactory.SetEntityId(existingConfig, Guid.NewGuid());
        _configs.Add(existingConfig);

        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 10,
            InitialRetryDelayMs: 5000,
            MaxRetryDelayMs: 300000,
            BackoffMultiplier: 4.0,
            CircuitBreakerFailureThreshold: 15,
            CircuitBreakerWindowMinutes: 20,
            CircuitBreakerOpenDurationMinutes: 120,
            AutoRetryDlq: true,
            DlqRetryIntervalHours: 24,
            EmailsEnabled: false,
            TestMode: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.MaxRetriesPerProvider.Should().Be(10);
        result.InitialRetryDelayMs.Should().Be(5000);
        result.MaxRetryDelayMs.Should().Be(300000);
        result.BackoffMultiplier.Should().Be(4.0);
        result.EmailsEnabled.Should().BeFalse();
        result.TestMode.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 3,
            InitialRetryDelayMs: 1000,
            MaxRetryDelayMs: 60000,
            BackoffMultiplier: 2.0,
            CircuitBreakerFailureThreshold: 5,
            CircuitBreakerWindowMinutes: 10,
            CircuitBreakerOpenDurationMinutes: 30,
            AutoRetryDlq: false,
            DlqRetryIntervalHours: 6,
            EmailsEnabled: true,
            TestMode: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 3,
            InitialRetryDelayMs: 1000,
            MaxRetryDelayMs: 60000,
            BackoffMultiplier: 2.0,
            CircuitBreakerFailureThreshold: 5,
            CircuitBreakerWindowMinutes: 10,
            CircuitBreakerOpenDurationMinutes: 30,
            AutoRetryDlq: false,
            DlqRetryIntervalHours: 6,
            EmailsEnabled: true,
            TestMode: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(
            "EMAIL_CONFIGURATION_UPDATED",
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailConfiguration",
            It.IsAny<Guid>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_CanDisableEmails()
    {
        // Arrange
        var existingConfig = EmailConfiguration.CreateDefault();
        TestDataFactory.SetEntityId(existingConfig, Guid.NewGuid());
        _configs.Add(existingConfig);

        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 3,
            InitialRetryDelayMs: 1000,
            MaxRetryDelayMs: 60000,
            BackoffMultiplier: 2.0,
            CircuitBreakerFailureThreshold: 5,
            CircuitBreakerWindowMinutes: 10,
            CircuitBreakerOpenDurationMinutes: 30,
            AutoRetryDlq: false,
            DlqRetryIntervalHours: 6,
            EmailsEnabled: false,
            TestMode: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.EmailsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CanEnableTestMode()
    {
        // Arrange
        var existingConfig = EmailConfiguration.CreateDefault();
        TestDataFactory.SetEntityId(existingConfig, Guid.NewGuid());
        _configs.Add(existingConfig);

        _mockContext.Setup(x => x.EmailConfigurations)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_configs).Object);

        var command = new UpdateEmailConfigurationCommand(
            MaxRetriesPerProvider: 3,
            InitialRetryDelayMs: 1000,
            MaxRetryDelayMs: 60000,
            BackoffMultiplier: 2.0,
            CircuitBreakerFailureThreshold: 5,
            CircuitBreakerWindowMinutes: 10,
            CircuitBreakerOpenDurationMinutes: 30,
            AutoRetryDlq: false,
            DlqRetryIntervalHours: 6,
            EmailsEnabled: true,
            TestMode: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TestMode.Should().BeTrue();
    }
}
