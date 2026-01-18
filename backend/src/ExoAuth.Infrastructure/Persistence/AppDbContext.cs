using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    // System entities
    public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    public DbSet<SystemPermission> SystemPermissions => Set<SystemPermission>();
    public DbSet<SystemUserPermission> SystemUserPermissions => Set<SystemUserPermission>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();
    public DbSet<SystemInvite> SystemInvites => Set<SystemInvite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<MfaBackupCode> MfaBackupCodes => Set<MfaBackupCode>();
    public DbSet<LoginPattern> LoginPatterns => Set<LoginPattern>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Passkey> Passkeys => Set<Passkey>();
    public DbSet<IpRestriction> IpRestrictions => Set<IpRestriction>();

    // Email entities
    public DbSet<EmailProvider> EmailProviders => Set<EmailProvider>();
    public DbSet<EmailConfiguration> EmailConfigurations => Set<EmailConfiguration>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailAnnouncement> EmailAnnouncements => Set<EmailAnnouncement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply snake_case naming convention for all tables and columns
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table name
            var tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            // Column names
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName is not null)
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }

            // Primary and foreign key names
            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName is not null)
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            // Foreign key constraint names
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (constraintName is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            // Index names
            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty is not null)
                {
                    updatedAtProperty.CurrentValue = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
