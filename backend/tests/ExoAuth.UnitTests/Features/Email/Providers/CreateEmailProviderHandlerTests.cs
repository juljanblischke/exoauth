using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Application.Features.Email.Providers.Commands.CreateEmailProvider;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Providers;

public sealed class CreateEmailProviderHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly CreateEmailProviderHandler _handler;
    private readonly List<EmailProvider> _providers;

    public CreateEmailProviderHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _providers = new List<EmailProvider>();
        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        _mockEncryptionService.Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns("encrypted-config");

        _handler = new CreateEmailProviderHandler(
            _mockContext.Object,
            _mockEncryptionService.Object,
            _mockAuditService.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_CreatesProvider_WithCorrectProperties()
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App",
            Host = "smtp.example.com",
            Port = 587
        };

        var command = new CreateEmailProviderCommand(
            Name: "Test SMTP Provider",
            Type: EmailProviderType.Smtp,
            Priority: 1,
            IsEnabled: true,
            Configuration: config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test SMTP Provider");
        result.Type.Should().Be(EmailProviderType.Smtp);
        result.Priority.Should().Be(1);
        result.IsEnabled.Should().BeTrue();
        result.TotalSent.Should().Be(0);
        result.TotalFailed.Should().Be(0);
        result.SuccessRate.Should().Be(100.0);
    }

    [Fact]
    public async Task Handle_EncryptsConfiguration()
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App",
            ApiKey = "secret-api-key"
        };

        var command = new CreateEmailProviderCommand(
            Name: "SendGrid",
            Type: EmailProviderType.SendGrid,
            Priority: 1,
            IsEnabled: true,
            Configuration: config);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockEncryptionService.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App"
        };

        var command = new CreateEmailProviderCommand(
            Name: "Test Provider",
            Type: EmailProviderType.Smtp,
            Priority: 1,
            IsEnabled: true,
            Configuration: config);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App"
        };

        var command = new CreateEmailProviderCommand(
            Name: "Test Provider",
            Type: EmailProviderType.Smtp,
            Priority: 1,
            IsEnabled: true,
            Configuration: config);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.EmailProviderCreated,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailProvider",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesDisabledProvider_WhenIsEnabledFalse()
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App"
        };

        var command = new CreateEmailProviderCommand(
            Name: "Disabled Provider",
            Type: EmailProviderType.Smtp,
            Priority: 1,
            IsEnabled: false,
            Configuration: config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmailProviderType.Smtp)]
    [InlineData(EmailProviderType.SendGrid)]
    [InlineData(EmailProviderType.Mailgun)]
    [InlineData(EmailProviderType.AmazonSes)]
    [InlineData(EmailProviderType.Resend)]
    [InlineData(EmailProviderType.Postmark)]
    public async Task Handle_SupportsAllProviderTypes(EmailProviderType providerType)
    {
        // Arrange
        var config = new EmailProviderConfigDto
        {
            FromEmail = "noreply@example.com",
            FromName = "Test App"
        };

        var command = new CreateEmailProviderCommand(
            Name: $"Test {providerType}",
            Type: providerType,
            Priority: 1,
            IsEnabled: true,
            Configuration: config);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.Should().Be(providerType);
    }
}
