using GreenCrescent.Application.Features.Sponsorships;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class SponsorshipService(
    ApplicationDbContext dbContext) : ISponsorshipService
{
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

        if (beneficiary.Status !=
            BeneficiaryStatus.WaitingForSponsor)
        {
            throw new InvalidOperationException(
                "يجب أن تكون حالة المكفول «بانتظار كافل» قبل إنشاء الكفالة.");
        }

        var hasActiveSponsorship =
            await dbContext.Sponsorships.AnyAsync(
                item =>
                    item.BeneficiaryId ==
                    request.BeneficiaryId &&
                    item.Status ==
                    SponsorshipStatus.Active,
                cancellationToken);

        if (hasActiveSponsorship)
        {
            throw new InvalidOperationException(
                "هذا المكفول لديه كفالة فعالة بالفعل.");
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

        var sponsorship = await dbContext.Sponsorships
            .Include(item => item.Beneficiary)
            .FirstOrDefaultAsync(
                item => item.Id == sponsorshipId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكفالة المطلوبة غير موجودة.");

        if (sponsorship.Status !=
            SponsorshipStatus.Active)
        {
            throw new InvalidOperationException(
                "الكفالة ليست فعالة.");
        }

        sponsorship.Status =
            SponsorshipStatus.Ended;
        sponsorship.UpdatedAtUtc = DateTime.UtcNow;

        sponsorship.Notes = AppendReason(
            sponsorship.Notes,
            reason);

        sponsorship.Beneficiary.Status =
            BeneficiaryStatus.WaitingForSponsor;

        sponsorship.Beneficiary.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
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