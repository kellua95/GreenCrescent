using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorCreditLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_BookNumber_ReceiptNumber",
                table: "FinancialEntries");

            migrationBuilder.AddColumn<decimal>(
                name: "CreditBalance",
                table: "Sponsors",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SponsorCreditTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SponsorId = table.Column<int>(type: "integer", nullable: false),
                    FinancialEntryId = table.Column<int>(type: "integer", nullable: true),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorCreditTransactions", x => x.Id);
                    table.CheckConstraint("CK_SponsorCreditTransaction_Amount", "\"Amount\" <> 0");
                    table.CheckConstraint("CK_SponsorCreditTransaction_BalanceAfter", "\"BalanceAfter\" >= 0");
                    table.ForeignKey(
                        name: "FK_SponsorCreditTransactions_FinancialEntries_FinancialEntryId",
                        column: x => x.FinancialEntryId,
                        principalTable: "FinancialEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorCreditTransactions_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sponsor_CreditBalance",
                table: "Sponsors",
                sql: "\"CreditBalance\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_Book_Receipt_Unique",
                table: "FinancialEntries",
                columns: new[] { "BookNumber", "ReceiptNumber" },
                unique: true,
                filter: "\"BookNumber\" IS NOT NULL AND \"ReceiptNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorCreditTransactions_FinancialEntryId",
                table: "SponsorCreditTransactions",
                column: "FinancialEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorCreditTransactions_SponsorId",
                table: "SponsorCreditTransactions",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorCreditTransactions_SponsorId_CreatedAtUtc",
                table: "SponsorCreditTransactions",
                columns: new[] { "SponsorId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SponsorCreditTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sponsor_CreditBalance",
                table: "Sponsors");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEntries_Book_Receipt_Unique",
                table: "FinancialEntries");

            migrationBuilder.DropColumn(
                name: "CreditBalance",
                table: "Sponsors");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntries_BookNumber_ReceiptNumber",
                table: "FinancialEntries",
                columns: new[] { "BookNumber", "ReceiptNumber" });
        }
    }
}
