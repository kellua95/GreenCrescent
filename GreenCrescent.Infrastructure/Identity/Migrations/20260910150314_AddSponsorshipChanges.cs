using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorshipChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SponsorshipChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    OldSponsorshipId = table.Column<int>(type: "integer", nullable: false),
                    NewSponsorshipId = table.Column<int>(type: "integer", nullable: true),
                    OldSponsorId = table.Column<int>(type: "integer", nullable: true),
                    NewSponsorId = table.Column<int>(type: "integer", nullable: true),
                    OldBeneficiaryId = table.Column<int>(type: "integer", nullable: true),
                    NewBeneficiaryId = table.Column<int>(type: "integer", nullable: true),
                    OldMonthlyAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    NewMonthlyAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipChanges_ChangeType",
                table: "SponsorshipChanges",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipChanges_EffectiveDate",
                table: "SponsorshipChanges",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipChanges_NewSponsorshipId",
                table: "SponsorshipChanges",
                column: "NewSponsorshipId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipChanges_OldSponsorshipId",
                table: "SponsorshipChanges",
                column: "OldSponsorshipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SponsorshipChanges");
        }
    }
}
