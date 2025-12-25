using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class SystemUserPermissionConfiguration : IEntityTypeConfiguration<SystemUserPermission>
{
    public void Configure(EntityTypeBuilder<SystemUserPermission> builder)
    {
        builder.ToTable("system_user_permissions");

        // Composite primary key
        builder.HasKey(x => new { x.SystemUserId, x.SystemPermissionId });

        builder.Property(x => x.GrantedAt)
            .IsRequired();

        builder.Property(x => x.GrantedBy);

        // Relationships
        builder.HasOne(x => x.SystemUser)
            .WithMany(u => u.Permissions)
            .HasForeignKey(x => x.SystemUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SystemPermission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(x => x.SystemPermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.SystemUserId);
        builder.HasIndex(x => x.SystemPermissionId);
    }
}
