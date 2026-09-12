namespace GreenCrescent.Application.Features.SponsorshipActions;

public sealed record ReplaceSponsorRequest(
    int SponsorshipId,
    int NewSponsorId,
    DateOnly EffectiveDate,
    string Reason);