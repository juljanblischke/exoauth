using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Fingerprint)
            .HasMaxLength(500);

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        // Device Info
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

        // Location
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

        // Status
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.TrustedAt);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.LastUsedAt)
            .IsRequired();

        // Approval
        builder.Property(x => x.ApprovalTokenHash)
            .HasMaxLength(100);

        builder.Property(x => x.ApprovalCodeHash)
            .HasMaxLength(100);

        builder.Property(x => x.ApprovalExpiresAt);

        builder.Property(x => x.ApprovalAttempts)
            .HasDefaultValue(0);

        builder.Property(x => x.RiskScore);

        builder.Property(x => x.RiskFactors)
            .HasMaxLength(500);

        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(x => x.IsApprovalExpired);
        builder.Ignore(x => x.IsPendingAndValid);
        builder.Ignore(x => x.IsTrusted);
        builder.Ignore(x => x.IsRevoked);
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.DisplayName);
        builder.Ignore(x => x.LocationDisplay);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastUsedAt);
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.ApprovalTokenHash);

        // Relationship with SystemUser
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
