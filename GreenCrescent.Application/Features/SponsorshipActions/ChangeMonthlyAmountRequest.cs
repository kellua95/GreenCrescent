namespace GreenCrescent.Application.Features.SponsorshipActions;

public sealed record ChangeMonthlyAmountRequest(
    int SponsorshipId,
    decimal NewMonthlyAmount,
    DateOnly EffectiveDate,
    string Reason);