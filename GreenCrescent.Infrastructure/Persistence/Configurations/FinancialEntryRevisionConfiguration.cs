using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations;

public sealed class FinancialEntryRevisionConfiguration
    : IEntityTypeConfiguration<FinancialEntryRevision>
{
    public void Configure(
        EntityTypeBuilder<FinancialEntryRevision> builder)
    {
        builder.ToTable("FinancialEntryRevisions");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.PerformedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(item => item.BeforeJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.AfterJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne(item => item.FinancialEntry)
            .WithMany()
            .HasForeignKey(item => item.FinancialEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new
        {
            item.FinancialEntryId,
            item.CreatedAtUtc
        });
    }
}