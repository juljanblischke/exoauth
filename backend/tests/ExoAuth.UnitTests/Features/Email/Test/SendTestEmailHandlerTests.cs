using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Test;

public sealed class SendTestEmailHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IEmailProviderFactory> _mockProviderFactory;
    private readonly Mock<ICircuitBreakerService> _mockCircuitBreaker;
    private readonly Mock<ILogger<SendTestEmailHandler>> _mockLogger;
    private readonly SendTestEmailHandler _handler;
    private readonly List<EmailProvider> _providers;
    private readonly List<EmailLog> _emailLogs;

    public SendTestEmailHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockProviderFactory = new Mock<IEmailProviderFactory>();
        _mockCircuitBreaker = new Mock<ICircuitBreakerService>();
        _mockLogger = new Mock<ILogger<SendTestEmailHandler>>();
        _providers = new List<EmailProvider>();
        _emailLogs = new List<EmailLog>();

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_emailLogs).Object);

        // Default: circuit breaker allows all providers
        _mockCircuitBreaker.Setup(x => x.CanUseProviderAsync(It.IsAny<EmailProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default: provider sends successfully
        var mockProviderImpl = new Mock<IEmailProviderImplementation>();
        mockProviderImpl.Setup(x => x.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockProviderFactory.Setup(x => x.CreateProvider(It.IsAny<EmailProvider>()))
            .Returns(mockProviderImpl.Object);

        _handler = new SendTestEmailHandler(
            _mockContext.Object,
            _mockProviderFactory.Object,
            _mockCircuitBreaker.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNoProvidersException_WhenNoProvidersConfigured()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailNoProvidersConfiguredException>();
    }

    [Fact]
    public async Task Handle_ThrowsProviderNotFoundException_WhenSpecificProviderNotFound()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailProviderNotFoundException>();
    }

    [Fact]
    public async Task Handle_SendsTestEmail_ViaSpecificProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test SMTP", EmailProviderType.Smtp);
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: providerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProviderUsedId.Should().Be(providerId);
        result.ProviderUsedName.Should().Be("Test SMTP");
    }

    [Fact]
    public async Task Handle_SendsTestEmail_ViaFirstEnabledProvider()
    {
        // Arrange
        var provider1Id = Guid.NewGuid();
        var provider2Id = Guid.NewGuid();

        var provider1 = TestDataFactory.CreateEmailProviderWithId(provider1Id, "Primary SendGrid", EmailProviderType.SendGrid, priority: 1);
        var provider2 = TestDataFactory.CreateEmailProviderWithId(provider2Id, "Backup SMTP", EmailProviderType.Smtp, priority: 2);

        _providers.AddRange(new[] { provider1, provider2 });

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProviderUsedId.Should().Be(provider1Id); // First by priority
        result.TotalProvidersAttempted.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SkipsDisabledProviders()
    {
        // Arrange
        var provider1Id = Guid.NewGuid();
        var provider2Id = Guid.NewGuid();

        var provider1 = TestDataFactory.CreateEmailProviderWithId(provider1Id, "Disabled Provider", priority: 1, isEnabled: false);
        var provider2 = TestDataFactory.CreateEmailProviderWithId(provider2Id, "Enabled Provider", priority: 2, isEnabled: true);

        _providers.AddRange(new[] { provider1, provider2 });

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProviderUsedId.Should().Be(provider2Id); // Only the enabled one
    }

    [Fact]
    public async Task Handle_SkipsProviders_WithOpenCircuitBreaker()
    {
        // Arrange
        var provider1Id = Guid.NewGuid();
        var provider2Id = Guid.NewGuid();

        var provider1 = TestDataFactory.CreateEmailProviderWithId(provider1Id, "Circuit Open", priority: 1);
        var provider2 = TestDataFactory.CreateEmailProviderWithId(provider2Id, "Available", priority: 2);

        _providers.AddRange(new[] { provider1, provider2 });

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        // First provider has circuit breaker open
        _mockCircuitBreaker.Setup(x => x.CanUseProviderAsync(
            It.Is<EmailProvider>(p => p.Id == provider1Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProviderUsedId.Should().Be(provider2Id); // Skipped the one with open circuit breaker
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenProviderThrowsException()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        // Provider throws exception
        var mockProviderImpl = new Mock<IEmailProviderImplementation>();
        mockProviderImpl.Setup(x => x.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        _mockProviderFactory.Setup(x => x.CreateProvider(It.IsAny<EmailProvider>()))
            .Returns(mockProviderImpl.Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: providerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task Handle_RecordsCircuitBreakerSuccess_WhenSendSucceeds()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCircuitBreaker.Verify(x => x.RecordSuccessAsync(
            It.Is<EmailProvider>(p => p.Id == providerId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RecordsCircuitBreakerFailure_WhenSendFails()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        // Provider throws exception
        var mockProviderImpl = new Mock<IEmailProviderImplementation>();
        mockProviderImpl.Setup(x => x.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Send failed"));

        _mockProviderFactory.Setup(x => x.CreateProvider(It.IsAny<EmailProvider>()))
            .Returns(mockProviderImpl.Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCircuitBreaker.Verify(x => x.RecordFailureAsync(
            It.Is<EmailProvider>(p => p.Id == providerId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsAttemptCount()
    {
        // Arrange
        var provider1Id = Guid.NewGuid();
        var provider2Id = Guid.NewGuid();

        var provider1 = TestDataFactory.CreateEmailProviderWithId(provider1Id, "Provider 1", priority: 1);
        var provider2 = TestDataFactory.CreateEmailProviderWithId(provider2Id, "Provider 2", priority: 2);

        _providers.AddRange(new[] { provider1, provider2 });

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new SendTestEmailCommand(
            RecipientEmail: "test@example.com",
            ProviderId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AttemptCount.Should().Be(1); // Succeeds on first attempt
        result.TotalProvidersAttempted.Should().Be(2);
    }
}
