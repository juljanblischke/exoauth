using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Helpers;

/// <summary>
/// Helper for creating mock DbContext for unit tests.
/// </summary>
public static class MockDbContext
{
    public static Mock<IAppDbContext> Create()
    {
        var mockContext = new Mock<IAppDbContext>();

        // Setup empty DbSets by default
        mockContext.Setup(x => x.SystemUsers).Returns(CreateAsyncMockDbSet<SystemUser>(new List<SystemUser>()).Object);
        mockContext.Setup(x => x.SystemPermissions).Returns(CreateAsyncMockDbSet<SystemPermission>(new List<SystemPermission>()).Object);
        mockContext.Setup(x => x.SystemUserPermissions).Returns(CreateAsyncMockDbSet<SystemUserPermission>(new List<SystemUserPermission>()).Object);
        mockContext.Setup(x => x.SystemAuditLogs).Returns(CreateAsyncMockDbSet<SystemAuditLog>(new List<SystemAuditLog>()).Object);
        mockContext.Setup(x => x.SystemInvites).Returns(CreateAsyncMockDbSet<SystemInvite>(new List<SystemInvite>()).Object);
        mockContext.Setup(x => x.RefreshTokens).Returns(CreateAsyncMockDbSet<RefreshToken>(new List<RefreshToken>()).Object);
        mockContext.Setup(x => x.Devices).Returns(CreateAsyncMockDbSet<Device>(new List<Device>()).Object);
        mockContext.Setup(x => x.PasswordResetTokens).Returns(CreateAsyncMockDbSet<PasswordResetToken>(new List<PasswordResetToken>()).Object);
        mockContext.Setup(x => x.MfaBackupCodes).Returns(CreateAsyncMockDbSet<MfaBackupCode>(new List<MfaBackupCode>()).Object);
        mockContext.Setup(x => x.LoginPatterns).Returns(CreateAsyncMockDbSet<LoginPattern>(new List<LoginPattern>()).Object);
        mockContext.Setup(x => x.Passkeys).Returns(CreateAsyncMockDbSet<Passkey>(new List<Passkey>()).Object);
        mockContext.Setup(x => x.IpRestrictions).Returns(CreateAsyncMockDbSet<IpRestriction>(new List<IpRestriction>()).Object);
        mockContext.Setup(x => x.MagicLinkTokens).Returns(CreateAsyncMockDbSet<MagicLinkToken>(new List<MagicLinkToken>()).Object);

        // Email DbSets
        mockContext.Setup(x => x.EmailProviders).Returns(CreateAsyncMockDbSet<EmailProvider>(new List<EmailProvider>()).Object);
        mockContext.Setup(x => x.EmailConfigurations).Returns(CreateAsyncMockDbSet<EmailConfiguration>(new List<EmailConfiguration>()).Object);
        mockContext.Setup(x => x.EmailLogs).Returns(CreateAsyncMockDbSet<EmailLog>(new List<EmailLog>()).Object);
        mockContext.Setup(x => x.EmailAnnouncements).Returns(CreateAsyncMockDbSet<EmailAnnouncement>(new List<EmailAnnouncement>()).Object);

        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return mockContext;
    }

    public static Mock<DbSet<T>> CreateMockDbSet<T>() where T : class
    {
        return CreateAsyncMockDbSet(new List<T>());
    }

    public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        return CreateAsyncMockDbSet(data);
    }

    /// <summary>
    /// Creates a mock DbSet that supports async operations like ToListAsync, FirstOrDefaultAsync, etc.
    /// </summary>
    public static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsAsyncQueryable();
        var mockSet = new Mock<DbSet<T>>();

        // Setup IAsyncEnumerable
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        // Setup IQueryable with async-compatible provider
        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(queryable.Provider);

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => data.GetEnumerator());

        // Setup AddAsync
        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .ReturnsAsync((T entity, CancellationToken _) => null!);

        // Setup Add
        mockSet.Setup(m => m.Add(It.IsAny<T>()))
            .Callback<T>(entity => data.Add(entity));

        // Setup Remove
        mockSet.Setup(m => m.Remove(It.IsAny<T>()))
            .Callback<T>(entity => data.Remove(entity));

        return mockSet;
    }
}
