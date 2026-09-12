namespace GreenCrescent.Application.Features.FinancialEntries;

public interface IFinancialEntryService
{
    Task<SponsorshipPaymentPreviewDto> PreviewSponsorshipPaymentAsync(
        int sponsorId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        CreateFinancialEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialEntryDto>> SearchAsync(
        string? searchTerm,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}