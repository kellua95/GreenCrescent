using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities;

public sealed class OrphanApplication : BaseEntity
{
    public string TrackingCode { get; set; } =
        Guid.NewGuid().ToString("N");

    // بيانات مقدم الطلب
    public string ApplicantName { get; set; } =
        string.Empty;

    public string ApplicantPhoneNumber { get; set; } =
        string.Empty;

    public string? ApplicantRelationship { get; set; }

    // بيانات اليتيم
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

    // بيانات الأب
    public DateOnly FatherDeathDate { get; set; }

    public string FatherDeathReason { get; set; } =
        string.Empty;

    // بيانات الأم
    public string MotherName { get; set; } =
        string.Empty;

    public string? MotherNationalNumber { get; set; }

    public DateOnly? MotherDateOfBirth { get; set; }

    public string? MotherNationality { get; set; }

    public bool IsMotherAlive { get; set; }

    public string? MotherPhoneNumber { get; set; }

    // بيانات المعيل الحالي
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

    // الأسرة
    public int FamilyMembersCount { get; set; }

    public int SponsoredFamilyMembersCount { get; set; }

    // الوضع الصحي
    public bool HasIllness { get; set; }

    public string? IllnessDescription { get; set; }

    public bool HasHealthInsurance { get; set; }

    public string? HealthInsuranceProvider { get; set; }

    // الوضع التعليمي
    public string? EducationStage { get; set; }

    public string? AcademicAchievement { get; set; }

    public string? SchoolDropoutReason { get; set; }

    public string? AlternativeDirection { get; set; }

    // الوضع المالي
    public decimal TotalMonthlyIncome { get; set; }

    public decimal TotalMonthlyExpenses { get; set; }

    // حالة الطلب
    public OrphanApplicationStatus Status { get; set; } =
        OrphanApplicationStatus.Submitted;

    public DateTime SubmittedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? DecisionReason { get; set; }

    public bool IsAutomaticallyRejected { get; set; }

    // المكفول الناتج عند الموافقة
    public int? BeneficiaryId { get; set; }

    public Beneficiary? Beneficiary { get; set; }

    public string? Notes { get; set; }

    public ICollection<OrphanApplicationFamilyMember>
        FamilyMembers
    { get; set; } =
            new List<OrphanApplicationFamilyMember>();

    public string GetOrphanFullName()
    {
        return string.Join(
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
    }
}