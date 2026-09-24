using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record OrphanApplicationDetailsDto(
    int Id,
    string TrackingCode,

    string ApplicantName,
    string ApplicantPhoneNumber,
    string? ApplicantRelationship,

    string OrphanFirstName,
    string OrphanFatherName,
    string? OrphanGrandfatherName,
    string OrphanFamilyName,
    string OrphanNationalNumber,
    DateOnly DateOfBirth,
    string Nationality,
    PersonGender Gender,
    string Address,
    string? PhoneNumber,

    DateOnly FatherDeathDate,
    string FatherDeathReason,

    string MotherName,
    string? MotherNationalNumber,
    DateOnly? MotherDateOfBirth,
    string? MotherNationality,
    bool IsMotherAlive,
    string? MotherPhoneNumber,

    string GuardianName,
    string GuardianNationalNumber,
    DateOnly? GuardianDateOfBirth,
    string? GuardianNationality,
    PersonGender? GuardianGender,
    string GuardianPhoneNumber,
    string? GuardianRelationship,

    int FamilyMembersCount,
    int SponsoredFamilyMembersCount,

    bool HasIllness,
    string? IllnessDescription,
    bool HasHealthInsurance,
    string? HealthInsuranceProvider,

    string? EducationStage,
    string? AcademicAchievement,
    string? SchoolDropoutReason,
    string? AlternativeDirection,

    decimal TotalMonthlyIncome,
    decimal TotalMonthlyExpenses,

    OrphanApplicationStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewedByUserId,
    string? DecisionReason,
    bool IsAutomaticallyRejected,
    int? BeneficiaryId,
    string? Notes,

    IReadOnlyList<OrphanApplicationFamilyMemberDto>
        FamilyMembers)
{
    public string OrphanFullName =>
        string.Join(
            " ",
            new[]
            {
                OrphanFirstName,
                OrphanFatherName,
                OrphanGrandfatherName,
                OrphanFamilyName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(value)));

    public byte[]? PhotoData { get; init; }

    public string? PhotoContentType { get; init; }
}