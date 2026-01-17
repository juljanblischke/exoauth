using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class MfaBackupCodeConfiguration : IEntityTypeConfiguration<MfaBackupCode>
{
    public void Configure(EntityTypeBuilder<MfaBackupCode> builder)
    {
        builder.ToTable("MfaBackupCodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsUsed)
            .IsRequired();

        builder.Property(x => x.UsedAt);

        // Index for fast lookup by UserId + not used
        builder.HasIndex(x => new { x.UserId, x.IsUsed })
            .HasDatabaseName("IX_MfaBackupCodes_UserId_IsUsed");

        // Relationship
        builder.HasOne(x => x.User)
            .WithMany(u => u.MfaBackupCodes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
