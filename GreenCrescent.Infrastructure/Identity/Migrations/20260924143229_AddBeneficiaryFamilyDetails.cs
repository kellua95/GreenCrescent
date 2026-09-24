using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryFamilyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyMembersCount",
                table: "Beneficiaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMonthlyIncome",
                table: "Beneficiaries",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.Sql(
    """
    UPDATE "Beneficiaries" AS b
    SET
        "FamilyMembersCount" =
            COALESCE(b."FamilyMembersCount", a."FamilyMembersCount"),
        "TotalMonthlyIncome" =
            COALESCE(b."TotalMonthlyIncome", a."TotalMonthlyIncome")
    FROM "OrphanApplications" AS a
    WHERE a."BeneficiaryId" = b."Id"
      AND a."Status" = 3
      AND (
          b."FamilyMembersCount" IS NULL
          OR b."TotalMonthlyIncome" IS NULL
      );
    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FamilyMembersCount",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "TotalMonthlyIncome",
                table: "Beneficiaries");
        }
    }
}
