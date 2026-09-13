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
    int ActiveSponsorshipsCount);