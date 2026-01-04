using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable("trusted_devices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeviceFingerprint)
            .HasMaxLength(500);

        builder.Property(x => x.Name)
            .HasMaxLength(200);

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

        builder.Property(x => x.TrustedAt)
            .IsRequired();

        builder.Property(x => x.LastUsedAt)
            .IsRequired();

        builder.Property(x => x.LastIpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.LastCountry)
            .HasMaxLength(100);

        builder.Property(x => x.LastCity)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => new { x.UserId, x.DeviceId });
        builder.HasIndex(x => new { x.UserId, x.DeviceFingerprint });
        builder.HasIndex(x => x.LastUsedAt);

        // Relationship with SystemUser
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
