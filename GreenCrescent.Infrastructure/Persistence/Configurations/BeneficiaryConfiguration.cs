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

            builder.Property(x => x.NationalNumber)
    .HasMaxLength(30);

            builder.Property(x => x.Gender)
                .HasConversion<int?>();

            builder.Property(x => x.Nationality)
                .HasMaxLength(100);

            builder.Property(x => x.Address)
                .HasMaxLength(500);

            builder.Property(x => x.GuardianName)
                .HasMaxLength(200);

            builder.Property(x => x.GuardianNationalNumber)
                .HasMaxLength(30);

            builder.Property(x => x.GuardianPhoneNumber)
                .HasMaxLength(30);

            builder.Property(x => x.GuardianRelationship)
                .HasMaxLength(100);

            builder.Property(x => x.FatherDeathDate)
                .HasColumnType("date");

            builder.Property(x => x.FatherDeathReason)
                .HasMaxLength(500);

            builder.Property(item => item.IsArchived)
                .HasDefaultValue(false);

            builder.Property(item => item.ArchiveReason)
                .HasMaxLength(1000);

            builder.HasIndex(item => item.IsArchived);

            builder.HasIndex(x => x.NationalNumber)
                .IsUnique()
                .HasFilter("\"NationalNumber\" IS NOT NULL")
                .HasDatabaseName(
                    "IX_Beneficiaries_NationalNumber_Unique");
        }
    }
}
