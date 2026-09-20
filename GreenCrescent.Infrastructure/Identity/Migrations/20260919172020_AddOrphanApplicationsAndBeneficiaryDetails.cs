using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GreenCrescent.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddOrphanApplicationsAndBeneficiaryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Beneficiaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FatherDeathDate",
                table: "Beneficiaries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherDeathReason",
                table: "Beneficiaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Beneficiaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "Beneficiaries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianNationalNumber",
                table: "Beneficiaries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianPhoneNumber",
                table: "Beneficiaries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianRelationship",
                table: "Beneficiaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalNumber",
                table: "Beneficiaries",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Beneficiaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrphanApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ApplicantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApplicantPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApplicantRelationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OrphanFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrphanFatherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrphanGrandfatherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OrphanFamilyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrphanNationalNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FatherDeathDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FatherDeathReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MotherName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MotherNationalNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MotherDateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    MotherNationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsMotherAlive = table.Column<bool>(type: "boolean", nullable: false),
                    MotherPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    GuardianName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GuardianNationalNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GuardianDateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    GuardianNationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuardianGender = table.Column<int>(type: "integer", nullable: true),
                    GuardianPhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GuardianRelationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FamilyMembersCount = table.Column<int>(type: "integer", nullable: false),
                    SponsoredFamilyMembersCount = table.Column<int>(type: "integer", nullable: false),
                    HasIllness = table.Column<bool>(type: "boolean", nullable: false),
                    IllnessDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HasHealthInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    HealthInsuranceProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EducationStage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcademicAchievement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SchoolDropoutReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AlternativeDirection = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotalMonthlyIncome = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalMonthlyExpenses = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAutomaticallyRejected = table.Column<bool>(type: "boolean", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrphanApplications", x => x.Id);
                    table.CheckConstraint("CK_OrphanApplication_FamilyMembersCount", "\"FamilyMembersCount\" >= 1");
                    table.CheckConstraint("CK_OrphanApplication_SponsoredFamilyMembersCount", "\"SponsoredFamilyMembersCount\" >= 0");
                    table.CheckConstraint("CK_OrphanApplication_SponsoredNotGreaterThanFamily", "\"SponsoredFamilyMembersCount\" <= \"FamilyMembersCount\"");
                    table.CheckConstraint("CK_OrphanApplication_TotalMonthlyExpenses", "\"TotalMonthlyExpenses\" >= 0");
                    table.CheckConstraint("CK_OrphanApplication_TotalMonthlyIncome", "\"TotalMonthlyIncome\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrphanApplications_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrphanApplicationFamilyMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrphanApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    Relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EducationalStatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SocialStatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrphanApplicationFamilyMembers", x => x.Id);
                    table.CheckConstraint("CK_OrphanApplicationFamilyMember_MonthlyIncome", "\"MonthlyIncome\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrphanApplicationFamilyMembers_OrphanApplications_OrphanApp~",
                        column: x => x.OrphanApplicationId,
                        principalTable: "OrphanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_NationalNumber_Unique",
                table: "Beneficiaries",
                column: "NationalNumber",
                unique: true,
                filter: "\"NationalNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplicationFamilyMembers_OrphanApplicationId",
                table: "OrphanApplicationFamilyMembers",
                column: "OrphanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplications_BeneficiaryId",
                table: "OrphanApplications",
                column: "BeneficiaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplications_OrphanNationalNumber_Unique",
                table: "OrphanApplications",
                column: "OrphanNationalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplications_Status",
                table: "OrphanApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplications_SubmittedAtUtc",
                table: "OrphanApplications",
                column: "SubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OrphanApplications_TrackingCode",
                table: "OrphanApplications",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrphanApplicationFamilyMembers");

            migrationBuilder.DropTable(
                name: "OrphanApplications");

            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_NationalNumber_Unique",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "FatherDeathDate",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "FatherDeathReason",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "GuardianNationalNumber",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "GuardianPhoneNumber",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "GuardianRelationship",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "NationalNumber",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Beneficiaries");
        }
    }
}
