using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialEntryRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "FinancialEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FinancialEntryRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialEntryId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: false),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEntryRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialEntryRevisions_FinancialEntries_FinancialEntryId",
                        column: x => x.FinancialEntryId,
                        principalTable: "FinancialEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntryRevisions_FinancialEntryId_CreatedAtUtc",
                table: "FinancialEntryRevisions",
                columns: new[] { "FinancialEntryId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialEntryRevisions");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                table: "FinancialEntries");
        }
    }
}
