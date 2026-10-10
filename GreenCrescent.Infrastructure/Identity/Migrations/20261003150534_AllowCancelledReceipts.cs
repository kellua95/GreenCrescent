using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AllowCancelledReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialEntry_Amount",
                table: "FinancialEntries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialEntry_Amount",
                table: "FinancialEntries",
                sql: "(\r\n    \"Status\" IN (1, 2)\r\n    AND \"Amount\" > 0\r\n)\r\nOR\r\n(\r\n    \"Status\" = 3\r\n    AND \"Amount\" = 0\r\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialEntry_Amount",
                table: "FinancialEntries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialEntry_Amount",
                table: "FinancialEntries",
                sql: "\"Amount\" > 0");
        }
    }
}
