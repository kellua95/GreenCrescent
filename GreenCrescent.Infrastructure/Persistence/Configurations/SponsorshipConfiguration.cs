using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations
{
    public sealed class SponsorshipConfiguration : IEntityTypeConfiguration<Sponsorship>
    {
        public void Configure(EntityTypeBuilder<Sponsorship> builder)
        {
            builder.ToTable(
                "Sponsorships",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_Sponsorship_MonthlyAmount",
                        "\"MonthlyAmount\" > 0");

                    table.HasCheckConstraint(
                        "CK_Sponsorship_Dates",
                        "\"EndDate\" >= \"StartDate\"");
                });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MonthlyAmount)
                .HasPrecision(18, 3)
                .IsRequired();

            builder.Property(x => x.StartDate)
                .HasColumnType("date");

            builder.Property(x => x.EndDate)
                .HasColumnType("date");

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.HasOne(x => x.Sponsor)
                .WithMany(x => x.Sponsorships)
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Beneficiary)
                .WithMany(x => x.Sponsorships)
                .HasForeignKey(x => x.BeneficiaryId)
                .OnDelete(DeleteBehavior.Restrict);

            // يمنع وجود كفالتين فعالتين للمكفول نفسه.
            builder.HasIndex(x => x.BeneficiaryId)
                .IsUnique()
                .HasFilter("\"Status\" = 1")
                .HasDatabaseName(
                    "IX_Sponsorships_OneActivePerBeneficiary");

            builder.HasIndex(x => new { x.SponsorId, x.Status });
            builder.HasIndex(x => x.EndDate);
        }
    }
}
