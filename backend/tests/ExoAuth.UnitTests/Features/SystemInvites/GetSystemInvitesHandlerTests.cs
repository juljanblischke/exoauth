using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.SystemInvites.Queries.GetSystemInvites;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.SystemInvites;

public sealed class GetSystemInvitesHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly GetSystemInvitesHandler _handler;
    private readonly DateTime _now = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public GetSystemInvitesHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _handler = new GetSystemInvitesHandler(
            _mockContext.Object,
            _mockDateTimeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithDefaultFilters_ReturnsPendingAndAcceptedInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var acceptedInvite = CreateInvite("accepted@example.com", inviter, acceptedAt: _now.AddDays(-1));
        var expiredInvite = CreateInvite("expired@example.com", inviter, expiresAt: _now.AddDays(-1));
        var revokedInvite = CreateInvite("revoked@example.com", inviter);
        revokedInvite.Revoke();

        var invites = new List<SystemInvite> { pendingInvite, acceptedInvite, expiredInvite, revokedInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Email == "pending@example.com");
        result.Items.Should().Contain(i => i.Email == "accepted@example.com");
        result.Items.Should().NotContain(i => i.Email == "expired@example.com");
        result.Items.Should().NotContain(i => i.Email == "revoked@example.com");
    }

    [Fact]
    public async Task Handle_WithIncludeExpired_ReturnsExpiredInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var expiredInvite = CreateInvite("expired@example.com", inviter, expiresAt: _now.AddDays(-1));

        var invites = new List<SystemInvite> { pendingInvite, expiredInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(IncludeExpired: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Email == "expired@example.com");
    }

    [Fact]
    public async Task Handle_WithIncludeRevoked_ReturnsRevokedInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var revokedInvite = CreateInvite("revoked@example.com", inviter);
        revokedInvite.Revoke();

        var invites = new List<SystemInvite> { pendingInvite, revokedInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(IncludeRevoked: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Email == "revoked@example.com");
    }

    [Fact]
    public async Task Handle_WithStatusFilterPending_ReturnsOnlyPendingInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var acceptedInvite = CreateInvite("accepted@example.com", inviter, acceptedAt: _now.AddDays(-1));

        var invites = new List<SystemInvite> { pendingInvite, acceptedInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Statuses: new List<string> { "pending" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(i => i.Status == "pending");
    }

    [Fact]
    public async Task Handle_WithStatusFilterMultiple_ReturnsMatchingInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var acceptedInvite = CreateInvite("accepted@example.com", inviter, acceptedAt: _now.AddDays(-1));
        var expiredInvite = CreateInvite("expired@example.com", inviter, expiresAt: _now.AddDays(-1));
        var revokedInvite = CreateInvite("revoked@example.com", inviter);
        revokedInvite.Revoke();

        var invites = new List<SystemInvite> { pendingInvite, acceptedInvite, expiredInvite, revokedInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Statuses: new List<string> { "pending", "revoked" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Status == "pending");
        result.Items.Should().Contain(i => i.Status == "revoked");
    }

    [Fact]
    public async Task Handle_WithSortByEmailAsc_ReturnsInvitesSortedByEmailAscending()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var inviteC = CreateInvite("charlie@example.com", inviter, expiresAt: _now.AddDays(1));
        var inviteA = CreateInvite("alice@example.com", inviter, expiresAt: _now.AddDays(1));
        var inviteB = CreateInvite("bob@example.com", inviter, expiresAt: _now.AddDays(1));

        var invites = new List<SystemInvite> { inviteC, inviteA, inviteB };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Sort: "email:asc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Email.Should().Be("alice@example.com");
        result.Items[1].Email.Should().Be("bob@example.com");
        result.Items[2].Email.Should().Be("charlie@example.com");
    }

    [Fact]
    public async Task Handle_WithSortByEmailDesc_ReturnsInvitesSortedByEmailDescending()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var inviteC = CreateInvite("charlie@example.com", inviter, expiresAt: _now.AddDays(1));
        var inviteA = CreateInvite("alice@example.com", inviter, expiresAt: _now.AddDays(1));
        var inviteB = CreateInvite("bob@example.com", inviter, expiresAt: _now.AddDays(1));

        var invites = new List<SystemInvite> { inviteC, inviteA, inviteB };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Sort: "email:desc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Email.Should().Be("charlie@example.com");
        result.Items[1].Email.Should().Be("bob@example.com");
        result.Items[2].Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_WithSortByCreatedAtDesc_ReturnsInvitesSortedByCreatedAtDescending()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite1 = CreateInvite("first@example.com", inviter, expiresAt: _now.AddDays(1));
        SetCreatedAt(invite1, _now.AddDays(-3));
        var invite2 = CreateInvite("second@example.com", inviter, expiresAt: _now.AddDays(1));
        SetCreatedAt(invite2, _now.AddDays(-1));
        var invite3 = CreateInvite("third@example.com", inviter, expiresAt: _now.AddDays(1));
        SetCreatedAt(invite3, _now.AddDays(-2));

        var invites = new List<SystemInvite> { invite1, invite2, invite3 };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Sort: "createdAt:desc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Email.Should().Be("second@example.com"); // -1 day (newest)
        result.Items[1].Email.Should().Be("third@example.com");  // -2 days
        result.Items[2].Email.Should().Be("first@example.com");  // -3 days (oldest)
    }

    [Fact]
    public async Task Handle_WithSearch_ReturnsMatchingInvites()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var inviteJohn = CreateInvite("john@example.com", inviter, firstName: "John", expiresAt: _now.AddDays(1));
        var inviteJane = CreateInvite("jane@example.com", inviter, firstName: "Jane", expiresAt: _now.AddDays(1));
        var inviteBob = CreateInvite("bob@example.com", inviter, firstName: "Bob", expiresAt: _now.AddDays(1));

        var invites = new List<SystemInvite> { inviteJohn, inviteJane, inviteBob };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Search: "john");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithInvalidSortField_DefaultsToCreatedAtWithOriginalDirection()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invite1 = CreateInvite("first@example.com", inviter, expiresAt: _now.AddDays(1));
        SetCreatedAt(invite1, _now.AddDays(-2));
        var invite2 = CreateInvite("second@example.com", inviter, expiresAt: _now.AddDays(1));
        SetCreatedAt(invite2, _now.AddDays(-1));

        var invites = new List<SystemInvite> { invite1, invite2 };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        // Invalid field but valid direction - should default field to createdAt, keep direction as asc
        var query = new GetSystemInvitesQuery(Sort: "invalidField:asc");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Should default to createdAt with original direction (asc)
        result.Items.Should().HaveCount(2);
        result.Items[0].Email.Should().Be("first@example.com"); // oldest first (asc)
        result.Items[1].Email.Should().Be("second@example.com"); // newest last
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(150, 100)]
    public async Task Handle_ClampsLimitToValidRange(int inputLimit, int expectedLimit)
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invites = new List<SystemInvite>();
        for (int i = 0; i < 105; i++)
        {
            var invite = CreateInvite($"user{i}@example.com", inviter, expiresAt: _now.AddDays(1));
            SetCreatedAt(invite, _now.AddMinutes(-i));
            invites.Add(invite);
        }

        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Limit: inputLimit);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(expectedLimit);
        result.Pagination.PageSize.Should().Be(expectedLimit);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectNextCursor()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var invites = new List<SystemInvite>();
        for (int i = 0; i < 5; i++)
        {
            var invite = CreateInvite($"user{i}@example.com", inviter, expiresAt: _now.AddDays(1));
            SetCreatedAt(invite, _now.AddMinutes(-i));
            invites.Add(invite);
        }

        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(Limit: 3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_StatusCalculation_ReturnsCorrectStatus()
    {
        // Arrange
        var inviter = TestDataFactory.CreateSystemUser("inviter@example.com");
        SetEntityId(inviter, Guid.NewGuid());

        var pendingInvite = CreateInvite("pending@example.com", inviter, expiresAt: _now.AddDays(1));
        var acceptedInvite = CreateInvite("accepted@example.com", inviter, acceptedAt: _now.AddDays(-1));
        var expiredInvite = CreateInvite("expired@example.com", inviter, expiresAt: _now.AddDays(-1));
        var revokedInvite = CreateInvite("revoked@example.com", inviter);
        revokedInvite.Revoke();

        var invites = new List<SystemInvite> { pendingInvite, acceptedInvite, expiredInvite, revokedInvite };
        _mockContext.Setup(x => x.SystemInvites)
            .Returns(MockDbContext.CreateAsyncMockDbSet(invites).Object);

        var query = new GetSystemInvitesQuery(IncludeExpired: true, IncludeRevoked: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items.Should().Contain(i => i.Email == "pending@example.com" && i.Status == "pending");
        result.Items.Should().Contain(i => i.Email == "accepted@example.com" && i.Status == "accepted");
        result.Items.Should().Contain(i => i.Email == "expired@example.com" && i.Status == "expired");
        result.Items.Should().Contain(i => i.Email == "revoked@example.com" && i.Status == "revoked");
    }

    // Helper methods
    private SystemInvite CreateInvite(
        string email,
        SystemUser inviter,
        string firstName = "Test",
        string lastName = "User",
        DateTime? expiresAt = null,
        DateTime? acceptedAt = null)
    {
        var invite = TestDataFactory.CreateSystemInvite(
            email,
            firstName,
            lastName,
            invitedBy: inviter.Id);

        SetEntityId(invite, Guid.NewGuid());
        SetInvitedByUser(invite, inviter);

        if (expiresAt.HasValue)
        {
            SetExpiresAt(invite, expiresAt.Value);
        }

        if (acceptedAt.HasValue)
        {
            invite.Accept();
        }

        return invite;
    }

    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var idField = typeof(T).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(entity, id);
    }

    private static void SetInvitedByUser(SystemInvite invite, SystemUser inviter)
    {
        var property = typeof(SystemInvite)
            .GetProperty("InvitedByUser");
        property?.SetValue(invite, inviter);
    }

    private static void SetExpiresAt(SystemInvite invite, DateTime expiresAt)
    {
        var property = typeof(SystemInvite)
            .GetProperty("ExpiresAt");
        property?.SetValue(invite, expiresAt);
    }

    private static void SetCreatedAt<T>(T entity, DateTime createdAt) where T : class
    {
        var property = typeof(T).BaseType?
            .GetProperty("CreatedAt");
        property?.SetValue(entity, createdAt);
    }
}
