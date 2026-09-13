using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleActiveSponsorships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sponsorships_OneActivePerBeneficiary",
                table: "Sponsorships");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_BeneficiaryId_Status",
                table: "Sponsorships",
                columns: new[] { "BeneficiaryId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sponsorships_BeneficiaryId_Status",
                table: "Sponsorships");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_OneActivePerBeneficiary",
                table: "Sponsorships",
                column: "BeneficiaryId",
                unique: true,
                filter: "\"Status\" = 1");
        }
    }
}
