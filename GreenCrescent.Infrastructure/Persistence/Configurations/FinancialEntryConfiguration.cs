using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations
{
    public sealed class FinancialEntryConfiguration : IEntityTypeConfiguration<FinancialEntry>
    {
        public void Configure(EntityTypeBuilder<FinancialEntry> builder)
        {
            builder.ToTable(
                "FinancialEntries",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_FinancialEntry_Amount",
                        "\"Amount\" > 0");
                });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DonorName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EntryType)
                .HasConversion<int>();

            builder.Property(x => x.Amount)
                .HasPrecision(18, 3)
                .IsRequired();

            builder.Property(x => x.PaymentMethod)
                .HasConversion<int>();

            builder.Property(x => x.BookNumber)
                .HasMaxLength(50);

            builder.Property(x => x.ReceiptNumber)
                .HasMaxLength(50);

            builder.Property(x => x.EntryDate)
                .HasColumnType("date");

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.Property(x => x.ReversalReason)
                .HasMaxLength(1000);

            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.EntryDate);
            builder.HasIndex(x => x.EntryType);
            builder.HasIndex(x => x.DonorName);
            builder.HasIndex(x => new
            {
                x.BookNumber,
                x.ReceiptNumber
            })
            .IsUnique()
            .HasFilter(
                "\"BookNumber\" IS NOT NULL AND " +
                "\"ReceiptNumber\" IS NOT NULL")
            .HasDatabaseName(
                "IX_FinancialEntries_Book_Receipt_Unique");
        }
    }
    }
