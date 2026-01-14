using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class UpdatePreferencesHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<IAuditService> _mockAuditService;

    public UpdatePreferencesHandlerTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockAuditService = new Mock<IAuditService>();

        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private UpdatePreferencesHandler CreateHandler() => new(
        _mockContext.Object,
        _mockCurrentUser.Object,
        _mockAuditService.Object);

    [Fact]
    public async Task Handle_WithValidLanguage_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdatePreferencesCommand("de-DE");

        var user = CreateUser(userId, "en-US");
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Language.Should().Be("de-DE");
        user.PreferredLanguage.Should().Be("de-DE");

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new UpdatePreferencesCommand("de-DE");

        _mockCurrentUser.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdatePreferencesCommand("de-DE");

        var users = new List<SystemUser>();

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_LogsAuditEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdatePreferencesCommand("de-DE");

        var user = CreateUser(userId, "en-US");
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.PreferencesUpdated,
            userId,
            null,
            "SystemUser",
            userId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public async Task Handle_WithDifferentLanguages_SetsCorrectLanguage(string language)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdatePreferencesCommand(language);

        var user = CreateUser(userId, language == "en-US" ? "de-DE" : "en-US");
        var users = new List<SystemUser> { user };

        SetupMockDbSet(users);
        _mockCurrentUser.Setup(x => x.UserId).Returns(userId);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Language.Should().Be(language);
        user.PreferredLanguage.Should().Be(language);
    }

    private void SetupMockDbSet(List<SystemUser> users)
    {
        var mockUsersDbSet = CreateAsyncMockDbSet(users);
        _mockContext.Setup(x => x.SystemUsers).Returns(mockUsersDbSet.Object);
    }

    private static SystemUser CreateUser(Guid userId, string language = "en-US")
    {
        var user = SystemUser.Create("test@example.com", "hash", "Test", "User", true);
        SetUserId(user, userId);
        user.SetPreferredLanguage(language);
        return user;
    }

    private static void SetUserId(SystemUser user, Guid userId)
    {
        var idField = typeof(SystemUser).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(user, userId);
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

        return mockSet;
    }
}
