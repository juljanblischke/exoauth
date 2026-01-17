using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProviders;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Providers;

public sealed class GetEmailProvidersHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetEmailProvidersHandler _handler;

    public GetEmailProvidersHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _handler = new GetEmailProvidersHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoProvidersExist()
    {
        // Arrange
        var query = new GetEmailProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsProvidersSortedByPriority()
    {
        // Arrange
        var provider1 = TestDataFactory.CreateEmailProviderWithId(
            Guid.NewGuid(), "Secondary SMTP", EmailProviderType.Smtp, priority: 2);
        var provider2 = TestDataFactory.CreateEmailProviderWithId(
            Guid.NewGuid(), "Primary SendGrid", EmailProviderType.SendGrid, priority: 1);
        var provider3 = TestDataFactory.CreateEmailProviderWithId(
            Guid.NewGuid(), "Backup Mailgun", EmailProviderType.Mailgun, priority: 3);

        var providers = new List<EmailProvider> { provider1, provider2, provider3 };
        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(providers).Object);

        var query = new GetEmailProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Primary SendGrid");
        result[0].Priority.Should().Be(1);
        result[1].Name.Should().Be("Secondary SMTP");
        result[1].Priority.Should().Be(2);
        result[2].Name.Should().Be("Backup Mailgun");
        result[2].Priority.Should().Be(3);
    }

    [Fact]
    public async Task Handle_CalculatesSuccessRateCorrectly()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");

        // Simulate 80 sent, 20 failed = 80% success rate
        for (int i = 0; i < 80; i++) provider.RecordSuccess();
        for (int i = 0; i < 20; i++) provider.RecordFailure();

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(new List<EmailProvider> { provider }).Object);

        var query = new GetEmailProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SuccessRate.Should().BeApproximately(80.0, 0.1);
        result[0].TotalSent.Should().Be(80);
        result[0].TotalFailed.Should().Be(20);
    }

    [Fact]
    public async Task Handle_IdentifiesCircuitBreakerOpen()
    {
        // Arrange
        var provider = TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "Test Provider");
        provider.OpenCircuitBreaker(30); // Open for 30 minutes

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(new List<EmailProvider> { provider }).Object);

        var query = new GetEmailProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsCircuitBreakerOpen.Should().BeTrue();
        result[0].CircuitBreakerOpenUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectProviderTypes()
    {
        // Arrange
        var providers = new List<EmailProvider>
        {
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "SMTP", EmailProviderType.Smtp, 1),
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "SendGrid", EmailProviderType.SendGrid, 2),
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "Mailgun", EmailProviderType.Mailgun, 3),
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "SES", EmailProviderType.AmazonSes, 4),
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "Resend", EmailProviderType.Resend, 5),
            TestDataFactory.CreateEmailProviderWithId(Guid.NewGuid(), "Postmark", EmailProviderType.Postmark, 6),
        };

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(providers).Object);

        var query = new GetEmailProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(6);
        result.Select(p => p.Type).Should().BeEquivalentTo(
            new[] { EmailProviderType.Smtp, EmailProviderType.SendGrid, EmailProviderType.Mailgun,
                    EmailProviderType.AmazonSes, EmailProviderType.Resend, EmailProviderType.Postmark });
    }
}
