using GreenCrescent.Application.Common;
using GreenCrescent.Application.Features.SponsorshipActions;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class SponsorshipActionService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ISponsorshipActionService
{
    private const int MaximumActiveSponsorshipsPerBeneficiary = 3;

    public async Task<int> ReplaceSponsorAsync(
        ReplaceSponsorRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var oldSponsorship = await dbContext.Sponsorships
            .Include(item => item.Beneficiary)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.SponsorshipId &&
                    item.Status == SponsorshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكفالة الفعالة المطلوبة غير موجودة.");

        ValidateReplacementDate(
            request.EffectiveDate,
            oldSponsorship);

        if (oldSponsorship.SponsorId ==
            request.NewSponsorId)
        {
            throw new InvalidOperationException(
                "الكافل الجديد هو الكافل الحالي نفسه.");
        }

        var newSponsor = await dbContext.Sponsors
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.NewSponsorId &&
                    item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل الجديد غير موجود أو موقوف.");

        var oldSponsorId = oldSponsorship.SponsorId;

        oldSponsorship.Status =
            SponsorshipStatus.Replaced;
        oldSponsorship.UpdatedAtUtc =
            DateTime.UtcNow;

        var newSponsorship = new Sponsorship
        {
            SponsorId = newSponsor.Id,

            BeneficiaryId =
        oldSponsorship.BeneficiaryId,

            ResponsibleSheikhId =
        oldSponsorship.ResponsibleSheikhId,

            MonthlyAmount =
        oldSponsorship.MonthlyAmount,

            StartDate =
        oldSponsorship.StartDate,

            EndDate =
        oldSponsorship.EndDate,

            Status =
        SponsorshipStatus.Active,

            Notes =
        $"استمرار كفالة بعد استبدال الكافل. السبب: {request.Reason.Trim()}"
        };

        dbContext.Sponsorships.Add(newSponsorship);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.SponsorshipChanges.Add(
            new SponsorshipChange
            {
                ChangeType =
                    SponsorshipChangeType.SponsorReplaced,
                OldSponsorshipId =
                    oldSponsorship.Id,
                NewSponsorshipId =
                    newSponsorship.Id,
                OldSponsorId = oldSponsorId,
                NewSponsorId = newSponsor.Id,
                OldBeneficiaryId =
                    oldSponsorship.BeneficiaryId,
                NewBeneficiaryId =
                    oldSponsorship.BeneficiaryId,
                OldMonthlyAmount =
                    oldSponsorship.MonthlyAmount,
                NewMonthlyAmount =
                    oldSponsorship.MonthlyAmount,
                EffectiveDate =
                    request.EffectiveDate,
                Reason = request.Reason.Trim(),
                PerformedByUserId =
                    await currentUserService.GetUserIdAsync()
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return newSponsorship.Id;
    }

    public async Task<int> ReplaceBeneficiaryAsync(
        ReplaceBeneficiaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var oldSponsorship = await dbContext.Sponsorships
            .Include(item => item.Beneficiary)
            .Include(item => item.Sponsor)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.SponsorshipId &&
                    item.Status == SponsorshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكفالة الفعالة المطلوبة غير موجودة.");

        ValidateReplacementDate(
            request.EffectiveDate,
            oldSponsorship);

        if (oldSponsorship.BeneficiaryId ==
            request.NewBeneficiaryId)
        {
            throw new InvalidOperationException(
                "المكفول الجديد هو المكفول الحالي نفسه.");
        }

        var newBeneficiary =
            await dbContext.Beneficiaries
                .FirstOrDefaultAsync(
                    item =>
                        item.Id ==
                        request.NewBeneficiaryId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول الجديد غير موجود.");

        if (newBeneficiary.IsArchived ||
            newBeneficiary.Status is not
                (BeneficiaryStatus.WaitingForSponsor or
                 BeneficiaryStatus.Sponsored))
        {
            throw new InvalidOperationException(
                "حالة المكفول الجديد لا تسمح باستبداله في الكفالة.");
        }

        var activeSponsorshipsCount =
            await dbContext.Sponsorships.CountAsync(
                item =>
                    item.BeneficiaryId ==
                    newBeneficiary.Id &&
                    item.Status ==
                    SponsorshipStatus.Active,
                cancellationToken);

        if (activeSponsorshipsCount >=
            MaximumActiveSponsorshipsPerBeneficiary)
        {
            throw new InvalidOperationException(
                $"المكفول الجديد وصل إلى الحد الأعلى وهو {MaximumActiveSponsorshipsPerBeneficiary} كفالات فعالة.");
        }

        var oldBeneficiaryId =
            oldSponsorship.BeneficiaryId;

        oldSponsorship.Status =
            SponsorshipStatus.Replaced;
        oldSponsorship.UpdatedAtUtc =
            DateTime.UtcNow;

        var hasOtherActiveSponsorships =
            await dbContext.Sponsorships.AnyAsync(
                item =>
                    item.Id != oldSponsorship.Id &&
                    item.BeneficiaryId ==
                    oldSponsorship.BeneficiaryId &&
                    item.Status ==
                    SponsorshipStatus.Active,
                cancellationToken);

        oldSponsorship.Beneficiary.UpdatedAtUtc =
            DateTime.UtcNow;

        if (hasOtherActiveSponsorships)
        {
            oldSponsorship.Beneficiary.Status =
                BeneficiaryStatus.Sponsored;
        }
        else
        {
            oldSponsorship.Beneficiary.Status =
                BeneficiaryStatus.NoLongerEligible;
            oldSponsorship.Beneficiary.IsArchived = true;
            oldSponsorship.Beneficiary.ArchivedAtUtc =
                DateTime.UtcNow;
            oldSponsorship.Beneficiary.ArchiveReason =
                $"تم استبداله في الكفالة المرتبطة بالكافل {oldSponsorship.Sponsor.Name}: {request.Reason.Trim()}";
        }

        newBeneficiary.Status =
            BeneficiaryStatus.Sponsored;
        newBeneficiary.UpdatedAtUtc =
            DateTime.UtcNow;

        var newSponsorship = new Sponsorship
        {
            SponsorId =
        oldSponsorship.SponsorId,

            BeneficiaryId =
        newBeneficiary.Id,

            ResponsibleSheikhId =
        oldSponsorship.ResponsibleSheikhId,

            MonthlyAmount =
        oldSponsorship.MonthlyAmount,

            StartDate =
        oldSponsorship.StartDate,

            EndDate =
        oldSponsorship.EndDate,

            Status =
        SponsorshipStatus.Active,

            Notes =
        $"استمرار كفالة بعد استبدال المكفول. السبب: {request.Reason.Trim()}"
        };

        dbContext.Sponsorships.Add(newSponsorship);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        dbContext.SponsorshipChanges.Add(
            new SponsorshipChange
            {
                ChangeType =
                    SponsorshipChangeType.BeneficiaryReplaced,
                OldSponsorshipId =
                    oldSponsorship.Id,
                NewSponsorshipId =
                    newSponsorship.Id,
                OldSponsorId =
                    oldSponsorship.SponsorId,
                NewSponsorId =
                    oldSponsorship.SponsorId,
                OldBeneficiaryId =
                    oldBeneficiaryId,
                NewBeneficiaryId =
                    newBeneficiary.Id,
                OldMonthlyAmount =
                    oldSponsorship.MonthlyAmount,
                NewMonthlyAmount =
                    oldSponsorship.MonthlyAmount,
                EffectiveDate =
                    request.EffectiveDate,
                Reason = request.Reason.Trim(),
                PerformedByUserId =
                    await currentUserService.GetUserIdAsync()
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return newSponsorship.Id;
    }

    public async Task ChangeMonthlyAmountAsync(
        ChangeMonthlyAmountRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);

        if (request.NewMonthlyAmount <= 0)
        {
            throw new InvalidOperationException(
                "قيمة الكفالة الجديدة يجب أن تكون أكبر من صفر.");
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var sponsorship = await dbContext.Sponsorships
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.SponsorshipId &&
                    item.Status == SponsorshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكفالة الفعالة المطلوبة غير موجودة.");

        if (request.EffectiveDate <
            sponsorship.StartDate)
        {
            throw new InvalidOperationException(
                "تاريخ التغيير لا يمكن أن يسبق بداية الكفالة.");
        }

        if (request.NewMonthlyAmount ==
            sponsorship.MonthlyAmount)
        {
            throw new InvalidOperationException(
                "القيمة الجديدة مساوية للقيمة الحالية.");
        }

        var oldAmount =
            sponsorship.MonthlyAmount;

        sponsorship.MonthlyAmount =
            request.NewMonthlyAmount;
        sponsorship.UpdatedAtUtc =
            DateTime.UtcNow;

        dbContext.SponsorshipChanges.Add(
            new SponsorshipChange
            {
                ChangeType =
                    SponsorshipChangeType.MonthlyAmountChanged,
                OldSponsorshipId = sponsorship.Id,
                NewSponsorshipId = sponsorship.Id,
                OldSponsorId = sponsorship.SponsorId,
                NewSponsorId = sponsorship.SponsorId,
                OldBeneficiaryId =
                    sponsorship.BeneficiaryId,
                NewBeneficiaryId =
                    sponsorship.BeneficiaryId,
                OldMonthlyAmount = oldAmount,
                NewMonthlyAmount =
                    request.NewMonthlyAmount,
                EffectiveDate =
                    request.EffectiveDate,
                Reason = request.Reason.Trim(),
                PerformedByUserId =
                    await currentUserService.GetUserIdAsync()
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "سبب الإجراء مطلوب.");
        }

        if (reason.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الإجراء يجب ألا يتجاوز 1000 حرف.");
        }
    }

    private static void ValidateReplacementDate(
    DateOnly effectiveDate,
    Sponsorship sponsorship)
    {
        if (effectiveDate < sponsorship.StartDate)
        {
            throw new InvalidOperationException(
                "تاريخ الاستبدال لا يمكن أن يسبق بداية الكفالة.");
        }
    }
}
