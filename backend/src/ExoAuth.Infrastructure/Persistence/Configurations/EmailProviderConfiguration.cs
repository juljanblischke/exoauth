using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class EmailProviderConfiguration : IEntityTypeConfiguration<EmailProvider>
{
    public void Configure(EntityTypeBuilder<EmailProvider> builder)
    {
        builder.ToTable("email_providers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ConfigurationEncrypted)
            .IsRequired();

        // Circuit Breaker
        builder.Property(x => x.FailureCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastFailureAt);

        builder.Property(x => x.CircuitBreakerOpenUntil);

        // Statistics
        builder.Property(x => x.TotalSent)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalFailed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastSuccessAt);

        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(x => x.IsCircuitBreakerOpen);
        builder.Ignore(x => x.CanBeUsed);
        builder.Ignore(x => x.SuccessRate);

        // Indexes
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.IsEnabled);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => new { x.IsEnabled, x.Priority });
    }
}
