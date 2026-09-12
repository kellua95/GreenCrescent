namespace GreenCrescent.Application.Features.SponsorshipActions;

public sealed record ReplaceBeneficiaryRequest(
    int SponsorshipId,
    int NewBeneficiaryId,
    DateOnly EffectiveDate,
    string Reason);