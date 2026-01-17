using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Infrastructure.Services;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Services;

public sealed class InviteCleanupServiceTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<InviteCleanupService>> _mockLogger;
    private readonly InviteCleanupService _service;
    private readonly DateTime _now = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly List<SystemInvite> _inviteData;

    public InviteCleanupServiceTests()
    {
        _mockContext = new Mock<IAppDbContext>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<InviteCleanupService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _inviteData = new List<SystemInvite>();

        // Setup mock DbSet with RemoveRange support
        var mockDbSet = CreateMockDbSetWithRemoveRange(_inviteData);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockDbSet.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new InviteCleanupService(
            _mockContext.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_WithNoInvites_ReturnsZero()
    {
        // Arrange - no invites

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(30, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_DeletesExpiredInvitesOlderThanRetentionPeriod()
    {
        // Arrange
        var retentionDays = 30;

        // Expired 40 days ago - should be deleted
        var oldExpiredInvite = CreateInvite("old@example.com", expiresAt: _now.AddDays(-40));

        // Expired 20 days ago - should NOT be deleted (within retention)
        var recentExpiredInvite = CreateInvite("recent@example.com", expiresAt: _now.AddDays(-20));

        // Still pending (not expired) - should NOT be deleted
        var pendingInvite = CreateInvite("pending@example.com", expiresAt: _now.AddDays(1));

        _inviteData.AddRange(new[] { oldExpiredInvite, recentExpiredInvite, pendingInvite });
        RefreshMockDbSet();

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(retentionDays, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _inviteData.Should().HaveCount(2);
        _inviteData.Should().Contain(i => i.Email == "recent@example.com");
        _inviteData.Should().Contain(i => i.Email == "pending@example.com");
        _inviteData.Should().NotContain(i => i.Email == "old@example.com");
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_DeletesRevokedInvitesOlderThanRetentionPeriod()
    {
        // Arrange
        var retentionDays = 30;

        // Revoked 40 days ago - should be deleted
        var oldRevokedInvite = CreateInvite("old-revoked@example.com", expiresAt: _now.AddDays(1));
        oldRevokedInvite.Revoke();
        SetRevokedAt(oldRevokedInvite, _now.AddDays(-40));

        // Revoked 20 days ago - should NOT be deleted (within retention)
        var recentRevokedInvite = CreateInvite("recent-revoked@example.com", expiresAt: _now.AddDays(1));
        recentRevokedInvite.Revoke();
        SetRevokedAt(recentRevokedInvite, _now.AddDays(-20));

        // Still pending - should NOT be deleted
        var pendingInvite = CreateInvite("pending@example.com", expiresAt: _now.AddDays(1));

        _inviteData.AddRange(new[] { oldRevokedInvite, recentRevokedInvite, pendingInvite });
        RefreshMockDbSet();

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(retentionDays, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _inviteData.Should().HaveCount(2);
        _inviteData.Should().Contain(i => i.Email == "recent-revoked@example.com");
        _inviteData.Should().Contain(i => i.Email == "pending@example.com");
        _inviteData.Should().NotContain(i => i.Email == "old-revoked@example.com");
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_DoesNotDeleteAcceptedInvites()
    {
        // Arrange
        var retentionDays = 30;

        // Accepted and "expired" 40 days ago - should NOT be deleted
        var acceptedInvite = CreateInvite("accepted@example.com", expiresAt: _now.AddDays(-40));
        acceptedInvite.Accept();

        // Expired 40 days ago (not accepted) - should be deleted
        var expiredInvite = CreateInvite("expired@example.com", expiresAt: _now.AddDays(-40));

        _inviteData.AddRange(new[] { acceptedInvite, expiredInvite });
        RefreshMockDbSet();

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(retentionDays, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _inviteData.Should().HaveCount(1);
        _inviteData.Should().Contain(i => i.Email == "accepted@example.com");
        _inviteData.Should().NotContain(i => i.Email == "expired@example.com");
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_DeletesBothExpiredAndRevokedOldInvites()
    {
        // Arrange
        var retentionDays = 30;

        // Old expired
        var oldExpired = CreateInvite("old-expired@example.com", expiresAt: _now.AddDays(-35));

        // Old revoked
        var oldRevoked = CreateInvite("old-revoked@example.com", expiresAt: _now.AddDays(1));
        oldRevoked.Revoke();
        SetRevokedAt(oldRevoked, _now.AddDays(-35));

        // Still pending
        var pending = CreateInvite("pending@example.com", expiresAt: _now.AddDays(1));

        _inviteData.AddRange(new[] { oldExpired, oldRevoked, pending });
        RefreshMockDbSet();

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(retentionDays, CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _inviteData.Should().HaveCount(1);
        _inviteData.Should().Contain(i => i.Email == "pending@example.com");
    }

    [Fact]
    public async Task CleanupExpiredAndRevokedInvitesAsync_RespectsRetentionDaysParameter()
    {
        // Arrange - use shorter retention period
        var retentionDays = 7;

        // Expired 10 days ago - should be deleted with 7 day retention
        var expiredInvite = CreateInvite("expired@example.com", expiresAt: _now.AddDays(-10));

        // Expired 5 days ago - should NOT be deleted (within 7 day retention)
        var recentExpiredInvite = CreateInvite("recent@example.com", expiresAt: _now.AddDays(-5));

        _inviteData.AddRange(new[] { expiredInvite, recentExpiredInvite });
        RefreshMockDbSet();

        // Act
        var result = await _service.CleanupExpiredAndRevokedInvitesAsync(retentionDays, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _inviteData.Should().HaveCount(1);
        _inviteData.Should().Contain(i => i.Email == "recent@example.com");
    }

    // Helper methods
    private SystemInvite CreateInvite(string email, DateTime expiresAt)
    {
        var invite = TestDataFactory.CreateSystemInvite(email);
        SetEntityId(invite, Guid.NewGuid());
        SetExpiresAt(invite, expiresAt);
        return invite;
    }

    private void RefreshMockDbSet()
    {
        var mockDbSet = CreateMockDbSetWithRemoveRange(_inviteData);
        _mockContext.Setup(x => x.SystemInvites).Returns(mockDbSet.Object);
    }

    private static Mock<DbSet<SystemInvite>> CreateMockDbSetWithRemoveRange(List<SystemInvite> data)
    {
        var mockSet = MockDbContext.CreateAsyncMockDbSet(data);

        // Add RemoveRange support
        mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<SystemInvite>>()))
            .Callback<IEnumerable<SystemInvite>>(entities =>
            {
                foreach (var entity in entities.ToList())
                {
                    data.Remove(entity);
                }
            });

        return mockSet;
    }

    private static void SetEntityId(SystemInvite invite, Guid id)
    {
        var idField = typeof(SystemInvite).BaseType?
            .GetField("<Id>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idField?.SetValue(invite, id);
    }

    private static void SetExpiresAt(SystemInvite invite, DateTime expiresAt)
    {
        var property = typeof(SystemInvite)
            .GetProperty("ExpiresAt");
        property?.SetValue(invite, expiresAt);
    }

    private static void SetRevokedAt(SystemInvite invite, DateTime revokedAt)
    {
        var property = typeof(SystemInvite)
            .GetProperty("RevokedAt");
        property?.SetValue(invite, revokedAt);
    }
}
