using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class SeparateBeneficiaryArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Beneficiaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Beneficiaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Beneficiaries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_IsArchived",
                table: "Beneficiaries",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_IsArchived",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Beneficiaries");
        }
    }
}
