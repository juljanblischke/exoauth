using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class DeviceSessionConfiguration : IEntityTypeConfiguration<DeviceSession>
{
    public void Configure(EntityTypeBuilder<DeviceSession> builder)
    {
        builder.ToTable("device_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeviceName)
            .HasMaxLength(200);

        builder.Property(x => x.DeviceFingerprint)
            .HasMaxLength(500);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.Browser)
            .HasMaxLength(100);

        builder.Property(x => x.BrowserVersion)
            .HasMaxLength(50);

        builder.Property(x => x.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(x => x.OsVersion)
            .HasMaxLength(50);

        builder.Property(x => x.DeviceType)
            .HasMaxLength(50);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.Country)
            .HasMaxLength(100);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(2);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.Latitude);

        builder.Property(x => x.Longitude);

        builder.Property(x => x.IsTrusted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.LastActivityAt)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => new { x.UserId, x.DeviceId });
        builder.HasIndex(x => x.IsRevoked);
        builder.HasIndex(x => x.LastActivityAt);
        builder.HasIndex(x => new { x.UserId, x.IsRevoked });

        // Relationship with SystemUser
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
