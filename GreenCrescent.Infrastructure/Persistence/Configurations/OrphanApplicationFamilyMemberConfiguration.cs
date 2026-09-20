using GreenCrescent.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GreenCrescent.Infrastructure.Persistence.Configurations;

public sealed class
    OrphanApplicationFamilyMemberConfiguration
    : IEntityTypeConfiguration<
        OrphanApplicationFamilyMember>
{
    public void Configure(
        EntityTypeBuilder<
            OrphanApplicationFamilyMember> builder)
    {
        builder.ToTable(
            "OrphanApplicationFamilyMembers",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_OrphanApplicationFamilyMember_MonthlyIncome",
                    "\"MonthlyIncome\" >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("date");

        builder.Property(x => x.Gender)
            .HasConversion<int?>();

        builder.Property(x => x.Relationship)
            .HasMaxLength(100);

        builder.Property(x => x.EducationalStatus)
            .HasMaxLength(200);

        builder.Property(x => x.SocialStatus)
            .HasMaxLength(200);

        builder.Property(x => x.HealthStatus)
            .HasMaxLength(500);

        builder.Property(x => x.Occupation)
            .HasMaxLength(200);

        builder.Property(x => x.MonthlyIncome)
            .HasPrecision(18, 3);

        builder.HasOne(x =>
                x.OrphanApplication)
            .WithMany(x =>
                x.FamilyMembers)
            .HasForeignKey(x =>
                x.OrphanApplicationId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasIndex(x =>
            x.OrphanApplicationId);
    }
}