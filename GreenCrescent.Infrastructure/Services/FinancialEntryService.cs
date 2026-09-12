using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Core.Services;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class FinancialEntryService(
    ApplicationDbContext dbContext) : IFinancialEntryService
{
    public async Task<SponsorshipPaymentPreviewDto>
        PreviewSponsorshipPaymentAsync(
            int sponsorId,
            decimal amount,
            CancellationToken cancellationToken = default)
    {
        var sponsor = await dbContext.Sponsors
            .AsNoTracking()
            .Where(item =>
                item.Id == sponsorId &&
                item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Name
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل غير موجود أو موقوف.");

        var sponsorships = await dbContext.Sponsorships
            .AsNoTracking()
            .Where(item =>
                item.SponsorId == sponsorId &&
                item.Status == SponsorshipStatus.Active)
            .OrderBy(item => item.Beneficiary.FileNumber)
            .Select(item => new
            {
                item.Id,
                item.MonthlyAmount,
                item.EndDate,
                item.Beneficiary.FileNumber,
                BeneficiaryName = item.Beneficiary.Name
            })
            .ToListAsync(cancellationToken);

        var addedMonths =
            SponsorshipPaymentCalculator.CalculateMonths(
                amount,
                sponsorships.Select(item =>
                    item.MonthlyAmount));

        var allocations = sponsorships
            .Select(item =>
                new SponsorshipAllocationPreviewDto(
                    item.Id,
                    item.FileNumber,
                    item.BeneficiaryName,
                    item.MonthlyAmount,
                    item.EndDate,
                    item.EndDate.AddMonths(addedMonths),
                    item.MonthlyAmount * addedMonths))
            .ToList();

        return new SponsorshipPaymentPreviewDto(
            sponsor.Id,
            sponsor.Name,
            sponsorships.Sum(item =>
                item.MonthlyAmount),
            amount,
            addedMonths,
            allocations);
    }

    public async Task<int> CreateAsync(
        CreateFinancialEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await EnsureReceiptIsNotDuplicatedAsync(
            request.BookNumber,
            request.ReceiptNumber,
            cancellationToken);

        if (IsSponsorshipPayment(request.EntryType))
        {
            return await CreateSponsorshipPaymentAsync(
                request,
                cancellationToken);
        }

        return await CreateDonationAsync(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEntryDto>> SearchAsync(
        string? searchTerm,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FinancialEntries
            .AsNoTracking()
            .AsQueryable();

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

        return await query
            .OrderByDescending(item => item.EntryDate)
            .ThenByDescending(item => item.Id)
            .Take(200)
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
                item.Notes))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> CreateDonationAsync(
        CreateFinancialEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DonorName))
        {
            throw new InvalidOperationException(
                "اسم المتبرع مطلوب.");
        }

        var entry = CreateEntry(
            request,
            sponsorId: null,
            donorName: request.DonorName.Trim());

        dbContext.FinancialEntries.Add(entry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }

    private async Task<int> CreateSponsorshipPaymentAsync(
        CreateFinancialEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.SponsorId.HasValue ||
            request.SponsorId.Value <= 0)
        {
            throw new InvalidOperationException(
                "يجب اختيار الكافل من السجلات.");
        }

        var sponsor = await dbContext.Sponsors
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.SponsorId.Value &&
                    item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل غير موجود أو موقوف.");

        var sponsorships = await dbContext.Sponsorships
            .Where(item =>
                item.SponsorId == sponsor.Id &&
                item.Status == SponsorshipStatus.Active)
            .OrderBy(item => item.BeneficiaryId)
            .ToListAsync(cancellationToken);

        var addedMonths =
            SponsorshipPaymentCalculator.CalculateMonths(
                request.Amount,
                sponsorships.Select(item =>
                    item.MonthlyAmount));

        var entry = CreateEntry(
            request,
            sponsor.Id,
            sponsor.Name);

        foreach (var sponsorship in sponsorships)
        {
            var previousEndDate = sponsorship.EndDate;
            var allocatedAmount =
                sponsorship.MonthlyAmount * addedMonths;

            sponsorship.ExtendByMonths(addedMonths);

            entry.Allocations.Add(new PaymentAllocation
            {
                Sponsorship = sponsorship,
                AllocatedAmount = allocatedAmount,
                AddedMonths = addedMonths,
                PreviousEndDate = previousEndDate,
                NewEndDate = sponsorship.EndDate
            });
        }

        dbContext.FinancialEntries.Add(entry);

        // SaveChanges ينفذ إضافة الحركة والتوزيعات
        // وتمديد جميع الكفالات كعملية واحدة.
        await dbContext.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }

    private async Task EnsureReceiptIsNotDuplicatedAsync(
        string? bookNumber,
        string? receiptNumber,
        CancellationToken cancellationToken)
    {
        var normalizedBook =
            NormalizeOptional(bookNumber);
        var normalizedReceipt =
            NormalizeOptional(receiptNumber);

        if (normalizedBook is null ||
            normalizedReceipt is null)
        {
            return;
        }

        var exists = await dbContext.FinancialEntries
            .AnyAsync(
                item =>
                    item.BookNumber == normalizedBook &&
                    item.ReceiptNumber == normalizedReceipt &&
                    item.Status ==
                    FinancialEntryStatus.Confirmed,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "رقم الوصل مستخدم مسبقًا في هذا الدفتر.");
        }
    }

    private static FinancialEntry CreateEntry(
        CreateFinancialEntryRequest request,
        int? sponsorId,
        string donorName)
    {
        return new FinancialEntry
        {
            SponsorId = sponsorId,
            DonorName = donorName,
            EntryType = request.EntryType,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            BookNumber =
                NormalizeOptional(request.BookNumber),
            ReceiptNumber =
                NormalizeOptional(request.ReceiptNumber),
            EntryDate = request.EntryDate,
            Notes = NormalizeOptional(request.Notes),
            Status = FinancialEntryStatus.Confirmed
        };
    }

    private static void ValidateRequest(
        CreateFinancialEntryRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException(
                "القيمة المالية يجب أن تكون أكبر من صفر.");
        }

        if (!Enum.IsDefined(request.EntryType))
        {
            throw new InvalidOperationException(
                "نوع الإدخال المالي غير صحيح.");
        }

        if (!Enum.IsDefined(request.PaymentMethod))
        {
            throw new InvalidOperationException(
                "طريقة الدفع غير صحيحة.");
        }

        if (request.BookNumber?.Trim().Length > 50)
        {
            throw new InvalidOperationException(
                "رقم الدفتر يجب ألا يتجاوز 50 حرفًا.");
        }

        if (request.ReceiptNumber?.Trim().Length > 50)
        {
            throw new InvalidOperationException(
                "رقم الوصل يجب ألا يتجاوز 50 حرفًا.");
        }

        if (request.Notes?.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }
    }

    private static bool IsSponsorshipPayment(
        FinancialEntryType entryType)
    {
        return entryType is
            FinancialEntryType.OrphanSponsorship or
            FinancialEntryType.SponsorshipDeliveredByHand;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}