using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Providers.Commands.ResetCircuitBreaker;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Providers;

public sealed class ResetCircuitBreakerHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly ResetCircuitBreakerHandler _handler;
    private readonly List<EmailProvider> _providers;

    public ResetCircuitBreakerHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _providers = new List<EmailProvider>();

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        _handler = new ResetCircuitBreakerHandler(_mockContext.Object, _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenProviderDoesNotExist()
    {
        // Arrange
        var command = new ResetCircuitBreakerCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailProviderNotFoundException>();
    }

    [Fact]
    public async Task Handle_ResetsCircuitBreaker_WhenProviderExists()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        provider.OpenCircuitBreaker(30); // Open circuit breaker
        provider.RecordFailure(); // Add some failures
        provider.RecordFailure();
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        provider.IsCircuitBreakerOpen.Should().BeTrue();
        provider.FailureCount.Should().Be(2);

        var command = new ResetCircuitBreakerCommand(providerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsCircuitBreakerOpen.Should().BeFalse();
        result.FailureCount.Should().Be(0);
        result.CircuitBreakerOpenUntil.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new ResetCircuitBreakerCommand(providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new ResetCircuitBreakerCommand(providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(
            "EMAIL_PROVIDER_CIRCUIT_BREAKER_RESET",
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailProvider",
            providerId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsUpdatedProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new ResetCircuitBreakerCommand(providerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(providerId);
        result.Name.Should().Be("Test Provider");
    }
}
