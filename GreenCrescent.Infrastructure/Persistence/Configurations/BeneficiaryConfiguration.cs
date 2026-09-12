using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations
{
    public sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            builder.ToTable("Beneficiaries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FileNumber)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.Property(x => x.Status)
                .HasConversion<int>();

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            builder.HasIndex(x => x.FileNumber)
                .IsUnique();

            builder.HasIndex(x => x.Name);

            builder.Property(item => item.IsArchived)
                .HasDefaultValue(false);

            builder.Property(item => item.ArchiveReason)
                .HasMaxLength(1000);

            builder.HasIndex(item => item.IsArchived);
        }
    }
}
