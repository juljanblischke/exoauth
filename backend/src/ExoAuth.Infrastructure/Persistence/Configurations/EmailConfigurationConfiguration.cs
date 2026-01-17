using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class EmailConfigurationConfiguration : IEntityTypeConfiguration<EmailConfiguration>
{
    public void Configure(EntityTypeBuilder<EmailConfiguration> builder)
    {
        builder.ToTable("email_configuration");

        builder.HasKey(x => x.Id);

        // Retry Settings
        builder.Property(x => x.MaxRetriesPerProvider)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.InitialRetryDelayMs)
            .IsRequired()
            .HasDefaultValue(1000);

        builder.Property(x => x.MaxRetryDelayMs)
            .IsRequired()
            .HasDefaultValue(60000);

        builder.Property(x => x.BackoffMultiplier)
            .IsRequired()
            .HasDefaultValue(2.0);

        // Circuit Breaker Settings
        builder.Property(x => x.CircuitBreakerFailureThreshold)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(x => x.CircuitBreakerWindowMinutes)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(x => x.CircuitBreakerOpenDurationMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        // DLQ Settings
        builder.Property(x => x.AutoRetryDlq)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DlqRetryIntervalHours)
            .IsRequired()
            .HasDefaultValue(6);

        // General Settings
        builder.Property(x => x.EmailsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.TestMode)
            .IsRequired()
            .HasDefaultValue(false);

        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);
    }
}
