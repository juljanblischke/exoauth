using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Providers.Commands.DeleteEmailProvider;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Providers;

public sealed class DeleteEmailProviderHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly DeleteEmailProviderHandler _handler;
    private readonly List<EmailProvider> _providers;

    public DeleteEmailProviderHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _providers = new List<EmailProvider>();

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        _handler = new DeleteEmailProviderHandler(_mockContext.Object, _mockAuditService.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenProviderDoesNotExist()
    {
        // Arrange
        var command = new DeleteEmailProviderCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailProviderNotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletesProvider_WhenProviderExists()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider");
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new DeleteEmailProviderCommand(providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog_OnDeletion()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = TestDataFactory.CreateEmailProviderWithId(providerId, "Test Provider", EmailProviderType.SendGrid);
        _providers.Add(provider);

        _mockContext.Setup(x => x.EmailProviders)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_providers).Object);

        var command = new DeleteEmailProviderCommand(providerId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(
            "EMAIL_PROVIDER_DELETED",
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailProvider",
            providerId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
