using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Sponsorships;

public sealed record SponsorshipDto(
    int Id,
    int SponsorId,
    string SponsorName,
    int BeneficiaryId,
    int FileNumber,
    string BeneficiaryName,
    int? ResponsibleSheikhId,
    string? ResponsibleSheikhName,
    decimal MonthlyAmount,
    DateOnly StartDate,
    DateOnly EndDate,
    SponsorshipStatus Status,
    string? Notes);