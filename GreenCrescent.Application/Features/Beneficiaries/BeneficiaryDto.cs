using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed record BeneficiaryDto(
    int Id,
    int FileNumber,
    string Name,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    BeneficiaryStatus Status,
    bool IsArchived,
    string? ArchiveReason,
    string? Notes,
    int? ActiveSponsorshipId,
    string? ActiveSponsorName,
    int ActiveSponsorshipsCount)
{
    public string? NationalNumber { get; init; }

    public string? GuardianName { get; init; }

    public int? FamilyActiveSponsorshipsCount { get; init; }

    public string? GuardianNationalNumber { get; init; }

    public string? GuardianPhoneNumber { get; init; }

    public int? FamilyMembersCount { get; init; }

    public decimal? TotalMonthlyIncome { get; init; }

    public PersonGender? Gender { get; init; }

    public string? Nationality { get; init; }

    public string? Address { get; init; }

    public string? GuardianRelationship { get; init; }

    public DateOnly? FatherDeathDate { get; init; }

    public string? FatherDeathReason { get; init; }

    // ستُعبأ عند GetByIdAsync فقط.
    public byte[]? PhotoData { get; init; }

    public string? PhotoContentType { get; init; }
}