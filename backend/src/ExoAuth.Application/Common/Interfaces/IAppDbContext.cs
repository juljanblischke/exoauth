using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Database context interface for the application layer.
/// </summary>
public interface IAppDbContext
{
    DbSet<SystemUser> SystemUsers { get; }
    DbSet<SystemPermission> SystemPermissions { get; }
    DbSet<SystemUserPermission> SystemUserPermissions { get; }
    DbSet<SystemAuditLog> SystemAuditLogs { get; }
    DbSet<SystemInvite> SystemInvites { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<MfaBackupCode> MfaBackupCodes { get; }
    DbSet<LoginPattern> LoginPatterns { get; }
    DbSet<Device> Devices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
