using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations;

public sealed class SponsorCreditTransactionConfiguration :
    IEntityTypeConfiguration<SponsorCreditTransaction>
{
    public void Configure(
        EntityTypeBuilder<SponsorCreditTransaction> builder)
    {
        builder.ToTable(
            "SponsorCreditTransactions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_SponsorCreditTransaction_Amount",
                    "\"Amount\" <> 0");

                table.HasCheckConstraint(
                    "CK_SponsorCreditTransaction_BalanceAfter",
                    "\"BalanceAfter\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Sponsor)
            .WithMany(x => x.CreditTransactions)
            .HasForeignKey(x => x.SponsorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FinancialEntry)
            .WithMany(x => x.CreditTransactions)
            .HasForeignKey(x => x.FinancialEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SponsorId);

        builder.HasIndex(x => x.FinancialEntryId);

        builder.HasIndex(x => new
        {
            x.SponsorId,
            x.CreatedAtUtc
        });
    }
}