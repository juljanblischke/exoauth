using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.IpRestrictions.Commands.UpdateIpRestriction;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace ExoAuth.UnitTests.Features.IpRestrictions;

public sealed class UpdateIpRestrictionHandlerTests
{
    private readonly Mock<IAppDbContext> _mockDbContext;
    private readonly Mock<IIpRestrictionService> _mockIpRestrictionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly DateTime _testNow = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.NewGuid();
    private List<IpRestriction> _restrictions;
    private List<SystemUser> _users;

    public UpdateIpRestrictionHandlerTests()
    {
        _mockDbContext = new Mock<IAppDbContext>();
        _mockIpRestrictionService = new Mock<IIpRestrictionService>();
        _mockAuditService = new Mock<IAuditService>();

        _restrictions = new List<IpRestriction>();
        _users = new List<SystemUser>
        {
            CreateSystemUser(_userId, "admin@example.com")
        };

        SetupDbSets();
    }

    private void SetupDbSets()
    {
        var restrictionDbSet = CreateMockDbSet(_restrictions);
        var userDbSet = CreateMockDbSet(_users);
        _mockDbContext.Setup(x => x.IpRestrictions).Returns(restrictionDbSet.Object);
        _mockDbContext.Setup(x => x.SystemUsers).Returns(userDbSet.Object);
    }

    private UpdateIpRestrictionHandler CreateHandler()
    {
        return new UpdateIpRestrictionHandler(
            _mockDbContext.Object,
            _mockIpRestrictionService.Object,
            _mockAuditService.Object);
    }

    private static SystemUser CreateSystemUser(Guid id, string email, string firstName = "Admin", string lastName = "User")
    {
        var user = (SystemUser)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SystemUser));
        typeof(SystemUser).GetProperty(nameof(SystemUser.Id))!.SetValue(user, id);
        typeof(SystemUser).GetProperty(nameof(SystemUser.Email))!.SetValue(user, email);
        typeof(SystemUser).GetProperty(nameof(SystemUser.FirstName))!.SetValue(user, firstName);
        typeof(SystemUser).GetProperty(nameof(SystemUser.LastName))!.SetValue(user, lastName);
        return user;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUpdate_UpdatesRestriction()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Original reason",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Blacklist,
            "Updated reason",
            _testNow.AddDays(7),
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingRestriction.Id);
        result.IpAddress.Should().Be("192.168.1.100");
        result.Type.Should().Be(IpRestrictionType.Blacklist);
        result.Reason.Should().Be("Updated reason");
        result.ExpiresAt.Should().Be(_testNow.AddDays(7));

        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChangeTypeFromBlacklistToWhitelist_UpdatesSuccessfully()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Bad actor",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Whitelist,
            "Now trusted",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.Should().Be(IpRestrictionType.Whitelist);
        result.Reason.Should().Be("Now trusted");
    }

    [Fact]
    public async Task Handle_UpdateExpirationDate_UpdatesSuccessfully()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "10.0.0.0/8",
            IpRestrictionType.Whitelist,
            "Internal network",
            _testNow.AddDays(30),
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var newExpiry = _testNow.AddDays(60);
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Whitelist,
            "Internal network",
            newExpiry,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ExpiresAt.Should().Be(newExpiry);
    }

    [Fact]
    public async Task Handle_RemoveExpirationDate_UpdatesSuccessfully()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Temporary block",
            _testNow.AddDays(7),
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Blacklist,
            "Permanent block",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ExpiresAt.Should().BeNull();
        result.Reason.Should().Be("Permanent block");
    }

    [Fact]
    public async Task Handle_InvalidatesCacheAfterUpdate()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Test",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Blacklist,
            "Updated",
            null,
            _userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockIpRestrictionService.Verify(x => x.InvalidateCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LogsAuditAfterUpdate()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Original",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Whitelist,
            "Updated",
            _testNow.AddDays(7),
            _userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.IpRestrictionUpdated,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "IpRestriction",
            existingRestriction.Id,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task Handle_WhenRestrictionNotFound_ThrowsIpRestrictionNotFoundException()
    {
        // Arrange
        var handler = CreateHandler();
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateIpRestrictionCommand(
            nonExistentId,
            IpRestrictionType.Blacklist,
            "Test",
            null,
            _userId);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IpRestrictionNotFoundException>()
            .Where(e => e.RestrictionId == nonExistentId);
    }

    [Fact]
    public async Task Handle_WhenChangingType_UpdatesSuccessfully()
    {
        // Arrange
        var existingRestriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Originally blocked",
            null,
            Guid.NewGuid());
        _restrictions.Add(existingRestriction);
        SetupDbSets();

        var handler = CreateHandler();
        var command = new UpdateIpRestrictionCommand(
            existingRestriction.Id,
            IpRestrictionType.Whitelist, // Change type
            "Now trusted",
            null,
            _userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Type change should succeed (only one entry per IP allowed)
        result.Reason.Should().Be("Now trusted");
        result.Type.Should().Be(IpRestrictionType.Whitelist);
    }

    #endregion

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));

        return mockSet;
    }
}
