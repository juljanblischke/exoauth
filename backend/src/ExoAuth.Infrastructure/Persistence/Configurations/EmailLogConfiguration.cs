using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientUserId);

        builder.Property(x => x.RecipientEmail)
            .IsRequired()
            .HasMaxLength(320); // Max email length per RFC 5321

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TemplateName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TemplateVariables);

        builder.Property(x => x.Language)
            .IsRequired()
            .HasMaxLength(10);

        // Status Tracking
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.Property(x => x.SentViaProviderId);

        // Timestamps
        builder.Property(x => x.QueuedAt)
            .IsRequired();

        builder.Property(x => x.SentAt);

        builder.Property(x => x.FailedAt);

        builder.Property(x => x.MovedToDlqAt);

        builder.Property(x => x.AnnouncementId);

        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(x => x.IsInDlq);
        builder.Ignore(x => x.WasSent);
        builder.Ignore(x => x.CanRetry);

        // Indexes
        builder.HasIndex(x => x.RecipientUserId);
        builder.HasIndex(x => x.RecipientEmail);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TemplateName);
        builder.HasIndex(x => x.QueuedAt);
        builder.HasIndex(x => x.SentAt);
        builder.HasIndex(x => x.AnnouncementId);
        builder.HasIndex(x => new { x.Status, x.QueuedAt });

        // Relationships
        builder.HasOne(x => x.RecipientUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SentViaProvider)
            .WithMany()
            .HasForeignKey(x => x.SentViaProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Announcement)
            .WithMany(a => a.EmailLogs)
            .HasForeignKey(x => x.AnnouncementId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
