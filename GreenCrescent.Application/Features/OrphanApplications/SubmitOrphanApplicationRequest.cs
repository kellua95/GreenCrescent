using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed class SubmitOrphanApplicationRequest
{
    public string ApplicantName { get; set; } =
        string.Empty;

    public string ApplicantPhoneNumber { get; set; } =
        string.Empty;

    public string? ApplicantRelationship { get; set; }

    public string OrphanFirstName { get; set; } =
        string.Empty;

    public string OrphanFatherName { get; set; } =
        string.Empty;

    public string? OrphanGrandfatherName { get; set; }

    public string OrphanFamilyName { get; set; } =
        string.Empty;

    public string OrphanNationalNumber { get; set; } =
        string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Nationality { get; set; } =
        string.Empty;

    public PersonGender Gender { get; set; }

    public string Address { get; set; } =
        string.Empty;

    public string? PhoneNumber { get; set; }

    public byte[]? PhotoData { get; set; }

    public string? PhotoContentType { get; set; }

    public DateOnly FatherDeathDate { get; set; }

    public string FatherDeathReason { get; set; } =
        string.Empty;

    public string MotherName { get; set; } =
        string.Empty;

    public string? MotherNationalNumber { get; set; }

    public DateOnly? MotherDateOfBirth { get; set; }

    public string? MotherNationality { get; set; }

    public bool IsMotherAlive { get; set; }

    public string? MotherPhoneNumber { get; set; }

    public string GuardianName { get; set; } =
        string.Empty;

    public string GuardianNationalNumber { get; set; } =
        string.Empty;

    public DateOnly? GuardianDateOfBirth { get; set; }

    public string? GuardianNationality { get; set; }

    public PersonGender? GuardianGender { get; set; }

    public string GuardianPhoneNumber { get; set; } =
        string.Empty;

    public string? GuardianRelationship { get; set; }

    public int FamilyMembersCount { get; set; }

    public int SponsoredFamilyMembersCount { get; set; }

    public bool HasIllness { get; set; }

    public string? IllnessDescription { get; set; }

    public bool HasHealthInsurance { get; set; }

    public string? HealthInsuranceProvider { get; set; }

    public string? EducationStage { get; set; }

    public string? AcademicAchievement { get; set; }

    public string? SchoolDropoutReason { get; set; }

    public string? AlternativeDirection { get; set; }

    public decimal TotalMonthlyIncome { get; set; }

    public decimal TotalMonthlyExpenses { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<SubmitFamilyMemberRequest>
        FamilyMembers
    { get; set; } = [];
}