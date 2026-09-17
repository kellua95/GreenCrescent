using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations
{
    public sealed class SponsorConfiguration :
        IEntityTypeConfiguration<Sponsor>
    {
        public void Configure(
            EntityTypeBuilder<Sponsor> builder)
        {
            builder.ToTable(
                "Sponsors",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_Sponsor_CreditBalance",
                        "\"CreditBalance\" >= 0");
                });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.Address)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreditBalance)
                .HasPrecision(18, 3)
                .HasDefaultValue(0m)
                .IsRequired();

            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.PhoneNumber);
        }
    }
}