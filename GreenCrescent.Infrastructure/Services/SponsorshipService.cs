using GreenCrescent.Application.Common;
using GreenCrescent.Application.Common.Models;
using GreenCrescent.Application.Features.Sponsorships;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class SponsorshipService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : ISponsorshipService
{
    public async Task<PagedResult<SponsorshipDto>>
    SearchPageAsync(
        string? searchTerm,
        bool activeOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);

        pageSize = pageSize is 10 or 25 or 50
            ? pageSize
            : 10;

        var query = dbContext.Sponsorships
            .AsNoTracking()
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(item =>
                item.Status == SponsorshipStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            var isFileNumber =
                int.TryParse(term, out var fileNumber);

            query = query.Where(item =>
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

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(item => item.EndDate)
            .ThenBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new SponsorshipDto(
                item.Id,

                item.SponsorId,
                item.Sponsor.Name,

                item.BeneficiaryId,
                item.Beneficiary.FileNumber,
                item.Beneficiary.Name,

                item.ResponsibleSheikhId,
                item.ResponsibleSheikh != null
                    ? item.ResponsibleSheikh.Name
                    : null,
                
                item.MonthlyAmount,
                item.StartDate,
                item.EndDate,
                item.Status,
                item.Notes))
            .ToListAsync(cancellationToken);

        return new PagedResult<SponsorshipDto>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<IReadOnlyList<SponsorshipDto>> SearchAsync(
        string? searchTerm,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sponsorships
            .AsNoTracking()
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(sponsorship =>
                sponsorship.Status ==
                SponsorshipStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var isFileNumber =
                int.TryParse(term, out var fileNumber);

            query = query.Where(sponsorship =>
                EF.Functions.ILike(
                    sponsorship.Sponsor.Name,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    sponsorship.Beneficiary.Name,
                    $"%{term}%") ||
                (
                    isFileNumber &&
                    sponsorship.Beneficiary.FileNumber ==
                    fileNumber
                ));
        }

        return await query
            .OrderBy(sponsorship =>
                sponsorship.EndDate)
            .Take(100)
            .Select(sponsorship =>
                new SponsorshipDto(
                    sponsorship.Id,
                    sponsorship.SponsorId,
                    sponsorship.Sponsor.Name,
                    sponsorship.BeneficiaryId,
                    sponsorship.Beneficiary.FileNumber,
                    sponsorship.Beneficiary.Name,
                    sponsorship.ResponsibleSheikhId,
                    sponsorship.ResponsibleSheikh != null ? sponsorship.ResponsibleSheikh.Name : null,
                    sponsorship.MonthlyAmount,
                    sponsorship.StartDate,
                    sponsorship.EndDate,
                    sponsorship.Status,
                    sponsorship.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<SponsorshipDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sponsorships
            .AsNoTracking()
            .Where(sponsorship =>
                sponsorship.Id == id)
            .Select(sponsorship =>
                new SponsorshipDto(
                    sponsorship.Id,
                    sponsorship.SponsorId,
                    sponsorship.Sponsor.Name,
                    sponsorship.BeneficiaryId,
                    sponsorship.Beneficiary.FileNumber,
                    sponsorship.Beneficiary.Name,
                    sponsorship.ResponsibleSheikhId,
                    sponsorship.ResponsibleSheikh != null
                        ? sponsorship.ResponsibleSheikh.Name
                        : null,
                    sponsorship.MonthlyAmount,
                    sponsorship.StartDate,
                    sponsorship.EndDate,
                    sponsorship.Status,
                    sponsorship.Notes))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(
        CreateSponsorshipRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var sponsor = await dbContext.Sponsors
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.SponsorId &&
                    item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل غير موجود أو موقوف.");

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == request.BeneficiaryId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول غير موجود.");

        var responsibleSheikh =
            await dbContext.ResponsibleSheikhs
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == request.ResponsibleSheikhId &&
                        item.IsActive,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "المعرّف غير موجود أو موقوف.");

        if (beneficiary.Status is not
            (BeneficiaryStatus.WaitingForSponsor or
             BeneficiaryStatus.Sponsored))
        {
            throw new InvalidOperationException(
                "حالة المكفول لا تسمح بإنشاء كفالة جديدة.");
        }

        var activeSponsorshipsCount =
    await dbContext.Sponsorships.CountAsync(
        item =>
            item.BeneficiaryId ==
            request.BeneficiaryId &&
            item.Status ==
            SponsorshipStatus.Active,
        cancellationToken);

        if (activeSponsorshipsCount >= 3)
        {
            throw new InvalidOperationException(
                "هذا المكفول وصل إلى الحد الأعلى وهو 3 كفالات فعالة.");
        }

        var sponsorship = new Sponsorship
        {
            SponsorId = sponsor.Id,
            BeneficiaryId = beneficiary.Id,
            ResponsibleSheikhId = responsibleSheikh.Id,
            MonthlyAmount = request.MonthlyAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = SponsorshipStatus.Active,
            Notes = NormalizeOptional(request.Notes)
        };

        beneficiary.Status =
            BeneficiaryStatus.Sponsored;
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.Sponsorships.Add(sponsorship);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return sponsorship.Id;
    }

    public async Task StopAsync(
    int sponsorshipId,
    string reason,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "سبب إيقاف الكفالة مطلوب.");
        }

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الإيقاف يجب ألا يتجاوز 1000 حرف.");
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var sponsorship = await dbContext.Sponsorships
            .Include(item => item.Beneficiary)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == sponsorshipId &&
                    item.Status ==
                        SponsorshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكفالة الفعالة المطلوبة غير موجودة.");

        sponsorship.Status =
            SponsorshipStatus.Ended;

        sponsorship.UpdatedAtUtc =
            DateTime.UtcNow;

        sponsorship.Notes = AppendReason(
            sponsorship.Notes,
            normalizedReason);

        var hasAnotherActiveSponsorship =
            await dbContext.Sponsorships
                .AnyAsync(
                    item =>
                        item.Id != sponsorship.Id &&
                        item.BeneficiaryId ==
                            sponsorship.BeneficiaryId &&
                        item.Status ==
                            SponsorshipStatus.Active,
                    cancellationToken);

        sponsorship.Beneficiary.Status =
            hasAnotherActiveSponsorship
                ? BeneficiaryStatus.Sponsored
                : BeneficiaryStatus.WaitingForSponsor;

        sponsorship.Beneficiary.UpdatedAtUtc =
            DateTime.UtcNow;

        dbContext.SponsorshipChanges.Add(
            new SponsorshipChange
            {
                ChangeType =
                    SponsorshipChangeType.SponsorshipStopped,

                OldSponsorshipId =
                    sponsorship.Id,

                NewSponsorshipId =
                    null,

                OldSponsorId =
                    sponsorship.SponsorId,

                NewSponsorId =
                    null,

                OldBeneficiaryId =
                    sponsorship.BeneficiaryId,

                NewBeneficiaryId =
                    null,

                OldMonthlyAmount =
                    sponsorship.MonthlyAmount,

                NewMonthlyAmount =
                    null,

                EffectiveDate =
                    DateOnly.FromDateTime(
                        DateTime.Today),

                Reason =
                    normalizedReason,

                PerformedByUserId =
                    await currentUserService.GetUserIdAsync()
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateRequest(
        CreateSponsorshipRequest request)
    {
        if (request.SponsorId <= 0)
        {
            throw new InvalidOperationException(
                "يجب اختيار الكافل.");
        }

        if (request.BeneficiaryId <= 0)
        {
            throw new InvalidOperationException(
                "يجب اختيار المكفول.");
        }

        if (request.ResponsibleSheikhId <= 0)
        {
            throw new InvalidOperationException(
                "يجب اختيار المعرّف المسؤول عن الكفالة.");
        }

        if (request.MonthlyAmount <= 0)
        {
            throw new InvalidOperationException(
                "قيمة الكفالة يجب أن تكون أكبر من صفر.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException(
                "تاريخ النهاية يجب ألا يسبق تاريخ البداية.");
        }

        if (request.Notes?.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string AppendReason(
        string? currentNotes,
        string reason)
    {
        var stopNote =
            $"إيقاف الكفالة: {reason.Trim()}";

        return string.IsNullOrWhiteSpace(currentNotes)
            ? stopNote
            : $"{currentNotes}{Environment.NewLine}{stopNote}";
    }
}