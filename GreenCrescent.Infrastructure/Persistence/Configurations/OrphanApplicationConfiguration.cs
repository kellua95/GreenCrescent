using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations;

public sealed class OrphanApplicationConfiguration
    : IEntityTypeConfiguration<OrphanApplication>
{
    public void Configure(
        EntityTypeBuilder<OrphanApplication> builder)
    {
        builder.ToTable(
            "OrphanApplications",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_OrphanApplication_FamilyMembersCount",
                    "\"FamilyMembersCount\" >= 1");

                table.HasCheckConstraint(
                    "CK_OrphanApplication_SponsoredFamilyMembersCount",
                    "\"SponsoredFamilyMembersCount\" >= 0");

                table.HasCheckConstraint(
                    "CK_OrphanApplication_SponsoredNotGreaterThanFamily",
                    "\"SponsoredFamilyMembersCount\" <= \"FamilyMembersCount\"");

                table.HasCheckConstraint(
                    "CK_OrphanApplication_TotalMonthlyIncome",
                    "\"TotalMonthlyIncome\" >= 0");

                table.HasCheckConstraint(
                    "CK_OrphanApplication_TotalMonthlyExpenses",
                    "\"TotalMonthlyExpenses\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TrackingCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.TrackingCode)
            .IsUnique();

        builder.Property(x => x.ApplicantName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ApplicantPhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ApplicantRelationship)
            .HasMaxLength(100);

        builder.Property(x => x.OrphanFirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OrphanFatherName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OrphanGrandfatherName)
            .HasMaxLength(100);

        builder.Property(x => x.OrphanFamilyName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OrphanNationalNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x =>
            x.OrphanNationalNumber)
                .IsUnique()
                .HasDatabaseName(
                    "IX_OrphanApplications_OrphanNationalNumber_Unique");

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Nationality)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.FatherDeathDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.FatherDeathReason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.MotherName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MotherNationalNumber)
            .HasMaxLength(30);

        builder.Property(x => x.MotherDateOfBirth)
            .HasColumnType("date");

        builder.Property(x => x.MotherNationality)
            .HasMaxLength(100);

        builder.Property(x => x.MotherPhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.GuardianName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.GuardianNationalNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.GuardianDateOfBirth)
            .HasColumnType("date");

        builder.Property(x => x.GuardianNationality)
            .HasMaxLength(100);

        builder.Property(x => x.GuardianGender)
            .HasConversion<int?>();

        builder.Property(x => x.GuardianPhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.GuardianRelationship)
            .HasMaxLength(100);

        builder.Property(x => x.IllnessDescription)
            .HasMaxLength(1000);

        builder.Property(x => x.HealthInsuranceProvider)
            .HasMaxLength(200);

        builder.Property(x => x.EducationStage)
            .HasMaxLength(200);

        builder.Property(x => x.AcademicAchievement)
            .HasMaxLength(200);

        builder.Property(x => x.SchoolDropoutReason)
            .HasMaxLength(1000);

        builder.Property(x => x.AlternativeDirection)
            .HasMaxLength(500);

        builder.Property(x => x.TotalMonthlyIncome)
            .HasPrecision(18, 3);

        builder.Property(x => x.TotalMonthlyExpenses)
            .HasPrecision(18, 3);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.DecisionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.SubmittedAtUtc);

        builder.HasOne(x => x.Beneficiary)
            .WithOne()
            .HasForeignKey<OrphanApplication>(
                x => x.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}