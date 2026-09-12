using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations
{
    public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
    {
        public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
        {
            builder.ToTable(
                "PaymentAllocations",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_PaymentAllocation_Amount",
                        "\"AllocatedAmount\" > 0");

                    table.HasCheckConstraint(
                        "CK_PaymentAllocation_Months",
                        "\"AddedMonths\" > 0");

                    table.HasCheckConstraint(
                        "CK_PaymentAllocation_Dates",
                        "\"NewEndDate\" > \"PreviousEndDate\"");
                });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AllocatedAmount)
                .HasPrecision(18, 3);

            builder.Property(x => x.PreviousEndDate)
                .HasColumnType("date");

            builder.Property(x => x.NewEndDate)
                .HasColumnType("date");

            builder.HasOne(x => x.FinancialEntry)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.FinancialEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sponsorship)
                .WithMany()
                .HasForeignKey(x => x.SponsorshipId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.FinancialEntryId);

            builder.HasIndex(x => new
            {
                x.FinancialEntryId,
                x.SponsorshipId
            }).IsUnique();
        }
    }
}
