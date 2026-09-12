namespace GreenCrescent.Application.Features.Sponsorships;

public sealed record CreateSponsorshipRequest(
    int SponsorId,
    int BeneficiaryId,
    int ResponsibleSheikhId,
    decimal MonthlyAmount,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes);