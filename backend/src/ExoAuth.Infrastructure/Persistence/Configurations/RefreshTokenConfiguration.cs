using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.UserType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.TokenHash);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UserType);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.IsRevoked);
        builder.HasIndex(x => new { x.UserId, x.UserType, x.IsRevoked });
    }
}
