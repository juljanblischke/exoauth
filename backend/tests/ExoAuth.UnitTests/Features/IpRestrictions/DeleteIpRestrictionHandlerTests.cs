using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.IpRestrictions.Commands.DeleteIpRestriction;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace ExoAuth.UnitTests.Features.IpRestrictions;

public sealed class DeleteIpRestrictionHandlerTests
{
    private readonly Mock<IAppDbContext> _mockDbContext;
    private readonly Mock<IIpRestrictionService> _mockIpRestrictionService;
    private List<IpRestriction> _restrictions;

    public DeleteIpRestrictionHandlerTests()
    {
        _mockDbContext = new Mock<IAppDbContext>();
        _mockIpRestrictionService = new Mock<IIpRestrictionService>();
        _restrictions = new List<IpRestriction>();
        SetupDbSet();
    }

    private void SetupDbSet()
    {
        var mockDbSet = CreateMockDbSet(_restrictions);
        _mockDbContext.Setup(x => x.IpRestrictions).Returns(mockDbSet.Object);
    }

    private DeleteIpRestrictionHandler CreateHandler()
    {
        return new DeleteIpRestrictionHandler(
            _mockDbContext.Object,
            _mockIpRestrictionService.Object);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WhenRestrictionExists_DeletesRestriction()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Test",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet();

        var handler = CreateHandler();
        var command = new DeleteIpRestrictionCommand(restriction.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidatesCacheAfterDeletion()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Test",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet();

        var handler = CreateHandler();
        var command = new DeleteIpRestrictionCommand(restriction.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockIpRestrictionService.Verify(x => x.InvalidateCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CanDeleteBlacklistEntry()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "10.0.0.0/8",
            IpRestrictionType.Blacklist,
            "Network block",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet();

        var handler = CreateHandler();
        var command = new DeleteIpRestrictionCommand(restriction.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_CanDeleteWhitelistEntry()
    {
        // Arrange
        var restriction = IpRestriction.CreateManual(
            "192.168.0.0/16",
            IpRestrictionType.Whitelist,
            "Internal network",
            null,
            Guid.NewGuid());
        _restrictions.Add(restriction);
        SetupDbSet();

        var handler = CreateHandler();
        var command = new DeleteIpRestrictionCommand(restriction.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_CanDeleteAutoCreatedEntry()
    {
        // Arrange
        var restriction = IpRestriction.CreateAuto(
            "192.168.1.100",
            IpRestrictionType.Blacklist,
            "Auto-blocked",
            DateTime.UtcNow.AddHours(1));
        _restrictions.Add(restriction);
        SetupDbSet();

        var handler = CreateHandler();
        var command = new DeleteIpRestrictionCommand(restriction.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
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
