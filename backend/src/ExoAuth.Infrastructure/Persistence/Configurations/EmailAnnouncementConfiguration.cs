using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class EmailAnnouncementConfiguration : IEntityTypeConfiguration<EmailAnnouncement>
{
    public void Configure(EntityTypeBuilder<EmailAnnouncement> builder)
    {
        builder.ToTable("email_announcements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HtmlBody)
            .IsRequired();

        builder.Property(x => x.PlainTextBody);

        // Targeting
        builder.Property(x => x.TargetType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.TargetPermission)
            .HasMaxLength(100);

        builder.Property(x => x.TargetUserIds);

        // Statistics
        builder.Property(x => x.TotalRecipients)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SentCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.FailedCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Metadata
        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.SentAt);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // Base entity properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(x => x.IsDraft);
        builder.Ignore(x => x.CanBeModified);
        builder.Ignore(x => x.CanBeDeleted);
        builder.Ignore(x => x.Progress);

        // Indexes
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.SentAt);
        builder.HasIndex(x => x.CreatedAt);

        // Relationships
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
