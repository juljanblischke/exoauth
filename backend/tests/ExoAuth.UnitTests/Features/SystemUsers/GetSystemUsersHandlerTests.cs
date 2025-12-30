using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemUsers.Queries.GetSystemUsers;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemUsers;

public sealed class GetSystemUsersHandlerTests
{
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly GetSystemUsersHandler _handler;

    public GetSystemUsersHandlerTests()
    {
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _handler = new GetSystemUsersHandler(_mockUserRepository.Object);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllNonAnonymizedUsers()
    {
        // Arrange
        var users = new List<SystemUser>
        {
            TestDataFactory.CreateSystemUser(email: "user1@example.com"),
            TestDataFactory.CreateSystemUser(email: "user2@example.com")
        };

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 2));

        var query = new GetSystemUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);

        // Verify default IsAnonymized=false was passed
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            null,
            false, // IsAnonymized default
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_PassesFilterToRepository()
    {
        // Arrange
        var activeUser = TestDataFactory.CreateSystemUser(email: "active@example.com", isActive: true);
        var users = new List<SystemUser> { activeUser };

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                true, // isActive
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 1));

        var query = new GetSystemUsersQuery(IsActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().IsActive.Should().BeTrue();

        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            true,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsAnonymizedTrue_ShowsAnonymizedUsers()
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                true, // isAnonymized
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(IsAnonymized: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            null,
            true, // Shows anonymized
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsAnonymizedNull_ShowsAllUsers()
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                (bool?)null,
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(IsAnonymized: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            null,
            null, // No filter - shows all
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsLockedFilter_PassesFilterToRepository()
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                true, // isLocked
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(IsLocked: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            null,
            false,
            true, // IsLocked
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMfaEnabledFilter_PassesFilterToRepository()
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                true, // mfaEnabled
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(MfaEnabled: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            null,
            false,
            null,
            true, // MfaEnabled
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleFilters_PassesAllFiltersToRepository()
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                true, // isActive
                false, // isAnonymized
                false, // isLocked
                true, // mfaEnabled
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(
            IsActive: true,
            IsAnonymized: false,
            IsLocked: false,
            MfaEnabled: true
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            null,
            null,
            true,
            false,
            false,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchAndFilters_PassesBothToRepository()
    {
        // Arrange
        var user = TestDataFactory.CreateSystemUser(email: "admin@example.com");
        var users = new List<SystemUser> { user };

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                "admin",
                It.IsAny<List<Guid>?>(),
                true,
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 1));

        var query = new GetSystemUsersQuery(Search: "admin", IsActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Email.Should().Be("admin@example.com");

        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            20,
            null,
            "admin",
            null,
            true,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(150, 100)]
    public async Task Handle_ClampsLimitToValidRange(int inputLimit, int expectedLimit)
    {
        // Arrange
        var users = new List<SystemUser>();

        _mockUserRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<string?>(),
                expectedLimit,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, null, 0));

        var query = new GetSystemUsersQuery(Limit: inputLimit);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetPagedAsync(
            null,
            expectedLimit,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
