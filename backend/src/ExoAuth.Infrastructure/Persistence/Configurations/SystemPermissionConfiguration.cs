using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class SystemPermissionConfiguration : IEntityTypeConfiguration<SystemPermission>
{
    public void Configure(EntityTypeBuilder<SystemPermission> builder)
    {
        builder.ToTable("system_permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasIndex(x => x.Category);
    }
}
