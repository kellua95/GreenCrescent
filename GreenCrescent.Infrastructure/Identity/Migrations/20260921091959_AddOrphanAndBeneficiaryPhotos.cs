using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddOrphanAndBeneficiaryPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "OrphanApplications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "OrphanApplications",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "Beneficiaries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "Beneficiaries",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "OrphanApplications");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "OrphanApplications");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "Beneficiaries");
        }
    }
}
