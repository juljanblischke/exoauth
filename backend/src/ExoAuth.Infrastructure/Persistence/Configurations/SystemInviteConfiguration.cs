using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class SystemInviteConfiguration : IEntityTypeConfiguration<SystemInvite>
{
    public void Configure(EntityTypeBuilder<SystemInvite> builder)
    {
        builder.ToTable("system_invites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.PermissionIds)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.AcceptedAt);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.ResentAt);

        builder.Property(x => x.InvitedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Relationships
        builder.HasOne(x => x.InvitedByUser)
            .WithMany()
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.AcceptedAt);
        builder.HasIndex(x => x.RevokedAt);
    }
}
