using GreenCrescent.Application.Common.Models;
using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class BeneficiaryService(
    ApplicationDbContext dbContext) : IBeneficiaryService
{
    public async Task<IReadOnlyList<BeneficiaryDto>> SearchAsync(
        string? searchTerm,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Beneficiaries
            .AsNoTracking()
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(beneficiary =>
                !beneficiary.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var isFileNumber =
                int.TryParse(term, out var fileNumber);

            query = query.Where(beneficiary =>
                EF.Functions.ILike(
                    beneficiary.Name,
                    $"%{term}%") ||
                (
                    beneficiary.PhoneNumber != null &&
                    EF.Functions.ILike(
                        beneficiary.PhoneNumber,
                        $"%{term}%")
                ) ||
                (
                    isFileNumber &&
                    beneficiary.FileNumber == fileNumber
                ));
        }

        return await query
            .OrderBy(beneficiary => beneficiary.FileNumber)
            .Take(100)
            .Select(beneficiary => new BeneficiaryDto(
                beneficiary.Id,
                beneficiary.FileNumber,
                beneficiary.Name,
                beneficiary.PhoneNumber,
                beneficiary.DateOfBirth,
                beneficiary.Status,
                beneficiary.IsArchived,
                beneficiary.ArchiveReason,
                beneficiary.Notes,
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        (int?)sponsorship.Id)
                    .FirstOrDefault(),
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        sponsorship.Sponsor.Name)
                    .FirstOrDefault(),

                beneficiary.Sponsorships.Count(sponsorship =>
                    sponsorship.Status ==
                    SponsorshipStatus.Active))
            {
                NationalNumber = beneficiary.NationalNumber,
                GuardianName = beneficiary.GuardianName,
                GuardianNationalNumber = beneficiary.GuardianNationalNumber,
                GuardianPhoneNumber = beneficiary.GuardianPhoneNumber,
                FamilyMembersCount = beneficiary.FamilyMembersCount,
                TotalMonthlyIncome = beneficiary.TotalMonthlyIncome
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BeneficiaryDto>>
        SearchPageAsync(
            string? searchTerm,
            bool includeArchived,
            bool sponsorshipCountDescending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
            pageNumber = Math.Max(
                1,
                pageNumber);

            pageSize = pageSize is 10 or 25 or 50
                ? pageSize
                : 10;

            var query = dbContext.Beneficiaries
                .AsNoTracking()
                .AsQueryable();

            if (!includeArchived)
            {
                query = query.Where(item =>
                    !item.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                var isFileNumber =
                    int.TryParse(
                        term,
                        out var fileNumber);

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

            var totalCount = await query.CountAsync(
                cancellationToken);

        var orderedQuery = sponsorshipCountDescending
            ? query
                .OrderByDescending(item =>
                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active))
                .ThenBy(item => item.FileNumber)
                .ThenBy(item => item.Id)

            : query
                .OrderBy(item =>
                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active))
                .ThenBy(item => item.FileNumber)
                .ThenBy(item => item.Id);

        var items = await orderedQuery
                        .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                {
                    NationalNumber = item.NationalNumber,
                    GuardianName = item.GuardianName,
                    GuardianNationalNumber = item.GuardianNationalNumber,
                    GuardianPhoneNumber = item.GuardianPhoneNumber,
                    FamilyMembersCount = item.FamilyMembersCount,
                    TotalMonthlyIncome = item.TotalMonthlyIncome
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<BeneficiaryDto>(
                items,
                totalCount,
                pageNumber,
                pageSize);
        }

    public async Task<BeneficiaryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Beneficiaries
            .AsNoTracking()
            .Where(beneficiary => beneficiary.Id == id)
            .Select(beneficiary => new BeneficiaryDto(
                beneficiary.Id,
                beneficiary.FileNumber,
                beneficiary.Name,
                beneficiary.PhoneNumber,
                beneficiary.DateOfBirth,
                beneficiary.Status,
                beneficiary.IsArchived,
                beneficiary.ArchiveReason,
                beneficiary.Notes,
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        (int?)sponsorship.Id)
                    .FirstOrDefault(),
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        sponsorship.Sponsor.Name)
                    .FirstOrDefault(),
                beneficiary.Sponsorships.Count(sponsorship =>
                    sponsorship.Status ==
                    SponsorshipStatus.Active))
            {
                NationalNumber = beneficiary.NationalNumber,
                GuardianName = beneficiary.GuardianName,
                GuardianNationalNumber = beneficiary.GuardianNationalNumber,
                GuardianPhoneNumber = beneficiary.GuardianPhoneNumber,
                FamilyMembersCount = beneficiary.FamilyMembersCount,
                TotalMonthlyIncome = beneficiary.TotalMonthlyIncome
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(
        CreateBeneficiaryRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(
            request.FileNumber,
            request.Name,
            request.PhoneNumber,
            request.Notes);

        var fileNumberExists =
            await dbContext.Beneficiaries.AnyAsync(
                beneficiary =>
                    beneficiary.FileNumber ==
                    request.FileNumber,
                cancellationToken);

        if (fileNumberExists)
        {
            throw new InvalidOperationException(
                "رقم الملف مستخدم لمكفول آخر.");
        }

        var beneficiary = new Beneficiary
        {
            FileNumber = request.FileNumber,
            Name = request.Name.Trim(),
            PhoneNumber =
                NormalizeOptional(request.PhoneNumber),
            DateOfBirth = request.DateOfBirth,
            Notes = NormalizeOptional(request.Notes),
            Status = BeneficiaryStatus.WaitingForSponsor
        };

        dbContext.Beneficiaries.Add(beneficiary);

        await dbContext.SaveChangesAsync(cancellationToken);

        return beneficiary.Id;
    }

    public async Task UpdateAsync(
        UpdateBeneficiaryRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(
            request.FileNumber,
            request.Name,
            request.PhoneNumber,
            request.Notes);

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == request.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        var fileNumberExists =
            await dbContext.Beneficiaries.AnyAsync(
                item =>
                    item.FileNumber == request.FileNumber &&
                    item.Id != request.Id,
                cancellationToken);

        if (fileNumberExists)
        {
            throw new InvalidOperationException(
                "رقم الملف مستخدم لمكفول آخر.");
        }

        beneficiary.FileNumber = request.FileNumber;
        beneficiary.Name = request.Name.Trim();
        beneficiary.PhoneNumber =
            NormalizeOptional(request.PhoneNumber);
        beneficiary.DateOfBirth = request.DateOfBirth;
        beneficiary.Notes =
            NormalizeOptional(request.Notes);
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeStatusAsync(
        int id,
        BeneficiaryStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        if (newStatus == BeneficiaryStatus.Sponsored)
        {
            throw new InvalidOperationException(
                "تتحول حالة المكفول إلى مكفول تلقائيًا عند إنشاء كفالة.");
        }

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        var hasActiveSponsorship =
            await dbContext.Sponsorships.AnyAsync(
                sponsorship =>
                    sponsorship.BeneficiaryId == id &&
                    sponsorship.Status ==
                    SponsorshipStatus.Active,
                cancellationToken);

        if (hasActiveSponsorship)
        {
            throw new InvalidOperationException(
                "لا يمكن تغيير حالة مكفول لديه كفالة فعالة. أوقف الكفالة أو استبدل المكفول أولًا.");
        }

        beneficiary.Status = newStatus;
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
    int id,
    string reason,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "سبب الأرشفة مطلوب.");
        }

        if (reason.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الأرشفة يجب ألا يتجاوز 1000 حرف.");
        }

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        var hasActiveSponsorship =
            await dbContext.Sponsorships.AnyAsync(
                item =>
                    item.BeneficiaryId == id &&
                    item.Status == SponsorshipStatus.Active,
                cancellationToken);

        if (hasActiveSponsorship)
        {
            throw new InvalidOperationException(
                "لا يمكن أرشفة مكفول لديه كفالة فعالة.");
        }

        beneficiary.IsArchived = true;
        beneficiary.ArchivedAtUtc = DateTime.UtcNow;
        beneficiary.ArchiveReason = reason.Trim();
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        beneficiary.IsArchived = false;
        beneficiary.ArchivedAtUtc = null;
        beneficiary.ArchiveReason = null;
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(
        int fileNumber,
        string name,
        string? phoneNumber,
        string? notes)
    {
        if (fileNumber <= 0)
        {
            throw new InvalidOperationException(
                "رقم الملف يجب أن يكون أكبر من صفر.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "اسم المكفول مطلوب.");
        }

        if (name.Trim().Length > 200)
        {
            throw new InvalidOperationException(
                "اسم المكفول يجب ألا يتجاوز 200 حرف.");
        }

        if (phoneNumber?.Trim().Length > 30)
        {
            throw new InvalidOperationException(
                "رقم الهاتف يجب ألا يتجاوز 30 حرفًا.");
        }

        if (notes?.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}