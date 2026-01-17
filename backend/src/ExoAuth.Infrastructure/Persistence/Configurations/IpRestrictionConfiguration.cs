using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class IpRestrictionConfiguration : IEntityTypeConfiguration<IpRestriction>
{
    public void Configure(EntityTypeBuilder<IpRestriction> builder)
    {
        builder.ToTable("ip_restrictions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(50); // Supports IPv6 + CIDR notation (e.g., "xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx:xxxx/128")

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.CreatedByUserId);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.IpAddress);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => new { x.Type, x.ExpiresAt });

        // Relationship with SystemUser (optional - for manual entries)
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
