namespace GreenCrescent.Application.Features.SponsorshipActions;

public interface ISponsorshipActionService
{
    Task<int> ReplaceSponsorAsync(
        ReplaceSponsorRequest request,
        CancellationToken cancellationToken = default);

    Task<int> ReplaceBeneficiaryAsync(
        ReplaceBeneficiaryRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeMonthlyAmountAsync(
        ChangeMonthlyAmountRequest request,
        CancellationToken cancellationToken = default);
}