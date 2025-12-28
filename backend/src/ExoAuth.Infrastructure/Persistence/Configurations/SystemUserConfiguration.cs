using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class SystemUserConfiguration : IEntityTypeConfiguration<SystemUser>
{
    public void Configure(EntityTypeBuilder<SystemUser> builder)
    {
        builder.ToTable("system_users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.EmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.LastLoginAt);

        // MFA fields
        builder.Property(x => x.MfaEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.MfaSecret)
            .HasMaxLength(500); // Encrypted, so longer than raw secret

        builder.Property(x => x.MfaEnabledAt);

        // User preferences
        builder.Property(x => x.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.MfaEnabled);

        // Navigation - defined in other configurations
    }
}
