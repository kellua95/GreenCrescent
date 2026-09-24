using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed class BeneficiaryAdditionalData
{
    public string? NationalNumber { get; set; }

    public PersonGender? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? Address { get; set; }

    public string? GuardianName { get; set; }

    public string? GuardianNationalNumber { get; set; }

    public string? GuardianPhoneNumber { get; set; }

    public string? GuardianRelationship { get; set; }

    public DateOnly? FatherDeathDate { get; set; }

    public string? FatherDeathReason { get; set; }

    public int? FamilyMembersCount { get; set; }

    public decimal? TotalMonthlyIncome { get; set; }
}