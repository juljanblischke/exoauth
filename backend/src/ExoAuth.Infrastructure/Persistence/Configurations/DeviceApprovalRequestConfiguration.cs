using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class DeviceApprovalRequestConfiguration : IEntityTypeConfiguration<DeviceApprovalRequest>
{
    public void Configure(EntityTypeBuilder<DeviceApprovalRequest> builder)
    {
        builder.ToTable("device_approval_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.DeviceSessionId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.RiskScore)
            .IsRequired();

        builder.Property(x => x.RiskFactors)
            .IsRequired()
            .HasMaxLength(1000); // JSON array of risk factors

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.ResolvedAt);

        builder.Property(x => x.ResolvedBy)
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes - unique constraint on TokenHash to prevent collisions
        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => x.CodeHash);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.DeviceSessionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => new { x.UserId, x.Status, x.ExpiresAt });
        builder.HasIndex(x => new { x.DeviceSessionId, x.Status });

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DeviceSession)
            .WithMany()
            .HasForeignKey(x => x.DeviceSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
