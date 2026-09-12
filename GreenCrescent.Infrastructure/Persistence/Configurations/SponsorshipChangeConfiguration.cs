using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations;

public sealed class SponsorshipChangeConfiguration
    : IEntityTypeConfiguration<SponsorshipChange>
{
    public void Configure(
        EntityTypeBuilder<SponsorshipChange> builder)
    {
        builder.ToTable("SponsorshipChanges");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ChangeType)
            .HasConversion<int>();

        builder.Property(item => item.OldMonthlyAmount)
            .HasPrecision(18, 3);

        builder.Property(item => item.NewMonthlyAmount)
            .HasPrecision(18, 3);

        builder.Property(item => item.EffectiveDate)
            .HasColumnType("date");

        builder.Property(item => item.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.PerformedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(item => item.OldSponsorshipId);

        builder.HasIndex(item => item.NewSponsorshipId);

        builder.HasIndex(item => item.EffectiveDate);

        builder.HasIndex(item => item.ChangeType);
    }
}