using ExoAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExoAuth.Infrastructure.Persistence.Configurations;

public sealed class LoginPatternConfiguration : IEntityTypeConfiguration<LoginPattern>
{
    public void Configure(EntityTypeBuilder<LoginPattern> builder)
    {
        builder.ToTable("login_patterns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TypicalCountries)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue("[]");

        builder.Property(x => x.TypicalCities)
            .IsRequired()
            .HasMaxLength(1000)
            .HasDefaultValue("[]");

        builder.Property(x => x.TypicalHours)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue("[]");

        builder.Property(x => x.TypicalDeviceTypes)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue("[]");

        builder.Property(x => x.LastLoginAt);

        builder.Property(x => x.LastIpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.LastCountry)
            .HasMaxLength(100);

        builder.Property(x => x.LastCity)
            .HasMaxLength(100);

        builder.Property(x => x.LastLatitude);

        builder.Property(x => x.LastLongitude);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // Indexes - unique on UserId since each user has one pattern
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        // Relationship
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<LoginPattern>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
