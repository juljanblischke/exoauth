using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class SystemAuditLogConfiguration : IEntityTypeConfiguration<SystemAuditLog>
{
    public void Configure(EntityTypeBuilder<SystemAuditLog> builder)
    {
        builder.ToTable("system_audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityType)
            .HasMaxLength(100);

        builder.Property(x => x.EntityId);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.Details)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.EntityType);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.Action, x.CreatedAt });
    }
}
