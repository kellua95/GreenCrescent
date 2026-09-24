using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Reports;
using GreenCrescent.Application.Features.Sponsors;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext) : IReportService
{
    public async Task<IReadOnlyList<GeneralSponsorshipReportDto>>
        GetGeneralReportAsync(
            string? searchTerm,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sponsorships
            .AsNoTracking()
            .Where(item =>
                item.Status == SponsorshipStatus.Active);

        query = ApplySponsorshipSearch(
            query,
            searchTerm);

        return await query
            .OrderBy(item => item.Beneficiary.FileNumber)
            .Select(item =>
                new GeneralSponsorshipReportDto(
                    item.Id,
                    item.Beneficiary.FileNumber,
                    item.Beneficiary.Name,
                    item.ResponsibleSheikh.Name,
                    item.Sponsor.Name,
                    item.Sponsor.PhoneNumber,
                    item.MonthlyAmount,
                    item.StartDate,
                    item.EndDate)
                {
                    BeneficiaryDateOfBirth = item.Beneficiary.DateOfBirth,
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DetailedSponsorshipReportDto>>
        GetDetailedReportAsync(
            string? searchTerm,
            bool includeHistory,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sponsorships
            .AsNoTracking()
            .AsQueryable();

        if (!includeHistory)
        {
            query = query.Where(item =>
                item.Status == SponsorshipStatus.Active);
        }

        query = ApplySponsorshipSearch(
            query,
            searchTerm);

        var rows = await query
            .OrderBy(item => item.Beneficiary.FileNumber)
            .ThenByDescending(item => item.StartDate)
            .Select(item =>
                new DetailedSponsorshipReportDto(
                    item.Id,
                    item.Beneficiary.FileNumber,
                    item.Beneficiary.Name,
                    item.ResponsibleSheikh.Name,
                    null,
                    null,
                    null,
                    item.Beneficiary.PhoneNumber,
                    item.Sponsor.Name,
                    item.Sponsor.PhoneNumber,
                    item.Sponsor.Address,
                    item.MonthlyAmount,
                    item.StartDate,
                    item.EndDate,
                    item.Status,
                    item.Notes)
                {
                    BeneficiaryDateOfBirth = item.Beneficiary.DateOfBirth,
                })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return rows;
        }

        var replacementLinks =
            await dbContext.SponsorshipChanges
                .AsNoTracking()
                .Where(item =>
                    item.ChangeType ==
                        SponsorshipChangeType.SponsorReplaced ||
                    item.ChangeType ==
                        SponsorshipChangeType.BeneficiaryReplaced)
                .Where(item =>
                    item.NewSponsorshipId != null)
                .Select(item =>
                    new ReplacementLink(
                        item.ChangeType,
                        item.OldSponsorshipId,
                        item.NewSponsorshipId!.Value,
                        item.OldBeneficiaryId))
                .ToListAsync(cancellationToken);

        var previousBeneficiaryIds = replacementLinks
            .Where(item =>
                item.OldBeneficiaryId.HasValue)
            .Select(item =>
                item.OldBeneficiaryId!.Value)
            .Distinct()
            .ToList();

        var beneficiaryNames =
            await dbContext.Beneficiaries
                .AsNoTracking()
                .Where(item =>
                    previousBeneficiaryIds.Contains(item.Id))
                .ToDictionaryAsync(
                    item => item.Id,
                    item => item.Name,
                    cancellationToken);

        for (var index = 0; index < rows.Count; index++)
        {
            var names = GetPreviousBeneficiaryNames(
                rows[index].SponsorshipId,
                replacementLinks,
                beneficiaryNames);

            rows[index] = rows[index] with
            {
                FirstPreviousBeneficiaryName =
                    names.ElementAtOrDefault(0),

                SecondPreviousBeneficiaryName =
                    names.ElementAtOrDefault(1),

                ThirdPreviousBeneficiaryName =
                    names.ElementAtOrDefault(2)
            };
        }

        return rows;
    }

    public async Task<IReadOnlyList<SponsorDto>>
    GetSponsorsAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sponsors
            .AsNoTracking()
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(item =>
                item.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(item =>
                EF.Functions.ILike(
                    item.Name,
                    $"%{term}%") ||
                (
                    item.PhoneNumber != null &&
                    EF.Functions.ILike(
                        item.PhoneNumber,
                        $"%{term}%")
                ) ||
                (
                    item.Address != null &&
                    EF.Functions.ILike(
                        item.Address,
                        $"%{term}%")
                ));
        }

        return await query
            .OrderBy(item => item.Name)
            .Select(item => new SponsorDto(
                item.Id,
                item.Name,
                item.PhoneNumber,
                item.Address,
                item.IsActive,
                item.Sponsorships.Count(sponsorship =>
                    sponsorship.Status ==
                    SponsorshipStatus.Active),
                item.CreditBalance))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BeneficiaryDto>>
        GetBeneficiariesAsync(
            string? searchTerm,
            bool archivedOnly,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Beneficiaries
            .AsNoTracking()
            .Where(item =>
                item.IsArchived == archivedOnly);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            var isFileNumber =
                int.TryParse(term, out var fileNumber);

            query = query.Where(item =>
                EF.Functions.ILike(
                    item.Name,
                    $"%{term}%") ||
                (
                    item.PhoneNumber != null &&
                    EF.Functions.ILike(
                        item.PhoneNumber,
                        $"%{term}%")
                ) ||
                (
                    isFileNumber &&
                    item.FileNumber == fileNumber
                ));
        }

        var rows = await query
            .OrderBy(item => item.FileNumber)
            .Select(item => new BeneficiaryDto(
                item.Id,
                item.FileNumber,
                item.Name,
                item.PhoneNumber,
                item.DateOfBirth,
                item.Status,
                item.IsArchived,
                item.ArchiveReason,
                item.Notes,

                item.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .OrderBy(sponsorship =>
                        sponsorship.Id)
                    .Select(sponsorship =>
                        (int?)sponsorship.Id)
                    .FirstOrDefault(),

                item.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .OrderBy(sponsorship =>
                        sponsorship.Id)
                    .Select(sponsorship =>
                        sponsorship.Sponsor.Name)
                    .FirstOrDefault(),

                item.Sponsorships.Count(sponsorship =>
                    sponsorship.Status ==
                    SponsorshipStatus.Active))
            )
            .ToListAsync(cancellationToken);
        // نستخدم جميع الكفالات الفعالة، وليس نتائج البحث أو الصفحة فقط.
        var guardianNumbers = rows
            .Select(item => item.GuardianNationalNumber?.Trim())
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Select(number => number!)
            .Distinct()
            .ToList();

        if (guardianNumbers.Count == 0)
        {
            return rows;
        }

        var familyCounts = await dbContext.Sponsorships
            .AsNoTracking()
            .Where(sponsorship =>
                sponsorship.Status == SponsorshipStatus.Active &&
                sponsorship.Beneficiary.GuardianNationalNumber != null &&
                guardianNumbers.Contains(
                    sponsorship.Beneficiary.GuardianNationalNumber!.Trim()))
            .GroupBy(sponsorship =>
                sponsorship.Beneficiary.GuardianNationalNumber!.Trim())
            .Select(group => new
            {
                GuardianNationalNumber = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.GuardianNationalNumber,
                item => item.Count,
                cancellationToken);

        return rows
            .Select(item =>
            {
                var guardianNumber =
                    item.GuardianNationalNumber?.Trim();

                int? familyCount = null;

                if (!string.IsNullOrWhiteSpace(guardianNumber))
                {
                    familyCount = familyCounts.TryGetValue(
                        guardianNumber,
                        out var count)
                            ? count
                            : 0;
                }

                return item with
                {
                    FamilyActiveSponsorshipsCount = familyCount
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<GeneralSponsorshipReportDto>>
        GetExpiredSponsorshipsAsync(
            DateOnly selectedDate,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Sponsorships
            .AsNoTracking()
            .Where(item =>
                item.Status == SponsorshipStatus.Active &&
                item.EndDate <= selectedDate)
            .OrderBy(item => item.EndDate)
            .ThenBy(item => item.Beneficiary.FileNumber)
            .Select(item =>
                new GeneralSponsorshipReportDto(
                    item.Id,
                    item.Beneficiary.FileNumber,
                    item.Beneficiary.Name,
                    item.ResponsibleSheikh.Name,
                    item.Sponsor.Name,
                    item.Sponsor.PhoneNumber,
                    item.MonthlyAmount,
                    item.StartDate,
                    item.EndDate
                    )
                {
                    BeneficiaryDateOfBirth = item.Beneficiary.DateOfBirth,
                }
                )
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEntryDto>>
        GetFinancialEntriesAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.FinancialEntries
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFinancialFilters(
            query,
            fromDate,
            toDate,
            searchTerm);

        return await ProjectFinancialEntries(query)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEntryDto>>
        GetDonationsAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.FinancialEntries
            .AsNoTracking()
            .Where(item =>
                item.EntryType == FinancialEntryType.Zakat ||
                item.EntryType == FinancialEntryType.Charity ||
                item.EntryType == FinancialEntryType.Other);

        query = ApplyFinancialFilters(
            query,
            fromDate,
            toDate,
            searchTerm);

        return await ProjectFinancialEntries(query)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Core.Entities.Sponsorship>
        ApplySponsorshipSearch(
            IQueryable<Core.Entities.Sponsorship> query,
            string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var term = searchTerm.Trim();
        var isFileNumber =
            int.TryParse(term, out var fileNumber);

        return query.Where(item =>
            EF.Functions.ILike(
                item.Sponsor.Name,
                $"%{term}%") ||
            EF.Functions.ILike(
                item.Beneficiary.Name,
                $"%{term}%") ||
            (
                isFileNumber &&
                item.Beneficiary.FileNumber == fileNumber
            ));
    }

    private static IQueryable<Core.Entities.FinancialEntry>
        ApplyFinancialFilters(
            IQueryable<Core.Entities.FinancialEntry> query,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? searchTerm)
    {
        if (fromDate.HasValue)
        {
            query = query.Where(item =>
                item.EntryDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(item =>
                item.EntryDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(item =>
                EF.Functions.ILike(
                    item.DonorName,
                    $"%{term}%") ||
                (
                    item.BookNumber != null &&
                    EF.Functions.ILike(
                        item.BookNumber,
                        $"%{term}%")
                ) ||
                (
                    item.ReceiptNumber != null &&
                    EF.Functions.ILike(
                        item.ReceiptNumber,
                        $"%{term}%")
                ));
        }

        return query;
    }

    private static IQueryable<FinancialEntryDto>
        ProjectFinancialEntries(
            IQueryable<Core.Entities.FinancialEntry> query)
    {
        return query
            .OrderByDescending(item => item.EntryDate)
            .ThenByDescending(item => item.Id)
            .Select(item => new FinancialEntryDto(
                item.Id,
                item.SponsorId,
                item.DonorName,
                item.EntryType,
                item.Amount,
                item.PaymentMethod,
                item.BookNumber,
                item.ReceiptNumber,
                item.EntryDate,
                item.Status,
                item.Notes));
    }

    private static IReadOnlyList<string>
    GetPreviousBeneficiaryNames(
        int sponsorshipId,
        IReadOnlyList<ReplacementLink> links,
        IReadOnlyDictionary<int, string> beneficiaryNames)
    {
        var namesFromNewestToOldest =
            new List<string>();

        var visitedSponsorships =
            new HashSet<int>();

        var currentSponsorshipId =
            sponsorshipId;

        while (visitedSponsorships.Add(
            currentSponsorshipId))
        {
            var link = links.FirstOrDefault(item =>
                item.NewSponsorshipId ==
                currentSponsorshipId);

            if (link is null)
            {
                break;
            }

            if (link.ChangeType ==
                    SponsorshipChangeType.BeneficiaryReplaced &&
                link.OldBeneficiaryId.HasValue &&
                beneficiaryNames.TryGetValue(
                    link.OldBeneficiaryId.Value,
                    out var previousName))
            {
                namesFromNewestToOldest.Add(
                    previousName);
            }

            currentSponsorshipId =
                link.OldSponsorshipId;
        }

        namesFromNewestToOldest.Reverse();

        return namesFromNewestToOldest
            .Take(3)
            .ToList();
    }

    private sealed record ReplacementLink(
    SponsorshipChangeType ChangeType,
    int OldSponsorshipId,
    int NewSponsorshipId,
    int? OldBeneficiaryId);
}