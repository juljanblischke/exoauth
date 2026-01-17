using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class PasskeyConfiguration : IEntityTypeConfiguration<Passkey>
{
    public void Configure(EntityTypeBuilder<Passkey> builder)
    {
        builder.ToTable("Passkeys");

        builder.HasKey(x => x.Id);

        // WebAuthn Credential Data
        builder.Property(x => x.CredentialId)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.PublicKey)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.Counter)
            .IsRequired();

        builder.Property(x => x.CredType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.AaGuid)
            .IsRequired();

        // Metadata
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastUsedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Passkeys_UserId");

        builder.HasIndex(x => x.CredentialId)
            .IsUnique()
            .HasDatabaseName("IX_Passkeys_CredentialId");

        // Relationship
        builder.HasOne(x => x.User)
            .WithMany(u => u.Passkeys)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
