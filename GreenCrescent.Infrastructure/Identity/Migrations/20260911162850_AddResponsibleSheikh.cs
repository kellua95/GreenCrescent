using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibleSheikh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsibleSheikhId",
                table: "Sponsorships",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResponsibleSheikhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsibleSheikhs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sponsorships_ResponsibleSheikhId",
                table: "Sponsorships",
                column: "ResponsibleSheikhId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibleSheikhs_Name",
                table: "ResponsibleSheikhs",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sponsorships_ResponsibleSheikhs_ResponsibleSheikhId",
                table: "Sponsorships",
                column: "ResponsibleSheikhId",
                principalTable: "ResponsibleSheikhs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sponsorships_ResponsibleSheikhs_ResponsibleSheikhId",
                table: "Sponsorships");

            migrationBuilder.DropTable(
                name: "ResponsibleSheikhs");

            migrationBuilder.DropIndex(
                name: "IX_Sponsorships_ResponsibleSheikhId",
                table: "Sponsorships");

            migrationBuilder.DropColumn(
                name: "ResponsibleSheikhId",
                table: "Sponsorships");
        }
    }
}
