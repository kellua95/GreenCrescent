using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Application.Features.Sponsors;

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

    Task<IReadOnlyList<string>> GetFinancialBookNumbersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialEntryDto>>
        GetFinancialEntriesAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default,
            string? bookNumber = null);

    Task<IReadOnlyList<FinancialEntryDto>>
        GetDonationsAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default,
            string? bookNumber = null);

    Task<IReadOnlyList<SponsorDto>>
    GetSponsorsAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BeneficiaryDto>>
        GetBeneficiariesAsync(
            string? searchTerm,
            bool archivedOnly,
            CancellationToken cancellationToken = default);
}