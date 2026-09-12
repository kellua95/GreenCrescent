using GreenCrescent.Application.Features.FinancialEntries;

namespace GreenCrescent.Application.Features.Reports;

public interface IReportService
{
    Task<IReadOnlyList<GeneralSponsorshipReportDto>>
        GetGeneralReportAsync(
            string? searchTerm,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DetailedSponsorshipReportDto>>
        GetDetailedReportAsync(
            string? searchTerm,
            bool includeHistory,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneralSponsorshipReportDto>>
        GetExpiredSponsorshipsAsync(
            DateOnly selectedDate,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialEntryDto>>
        GetFinancialEntriesAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialEntryDto>>
        GetDonationsAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default);
}