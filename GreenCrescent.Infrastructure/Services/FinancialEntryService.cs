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
                item.Name,
                item.CreditBalance
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل غير موجود أو موقوف.");

        var sponsorships = await dbContext.Sponsorships
            .AsNoTracking()
            .Where(item =>
                item.SponsorId == sponsorId &&
                item.Status == SponsorshipStatus.Active)
            .OrderBy(item =>
                item.Beneficiary.FileNumber)
            .Select(item => new
            {
                item.Id,
                item.MonthlyAmount,
                item.EndDate,
                item.Beneficiary.FileNumber,
                BeneficiaryName =
                    item.Beneficiary.Name
            })
            .ToListAsync(cancellationToken);

        var calculation =
            SponsorshipPaymentCalculator
                .CalculateWithCredit(
                    sponsor.CreditBalance,
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
                    item.EndDate.AddMonths(
                        calculation.AddedMonths),
                    item.MonthlyAmount *
                        calculation.AddedMonths))
            .ToList();

        return new SponsorshipPaymentPreviewDto(
            sponsor.Id,
            sponsor.Name,
            calculation.TotalMonthlyAmount,
            amount,
            calculation.AddedMonths,
            allocations,
            calculation.PreviousBalance,
            calculation.AvailableAmount,
            calculation.AppliedAmount,
            calculation.RemainingBalance);
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

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var sponsorId =
            request.SponsorId.Value;

        // قفل سجل الكافل حتى تنتهي عملية احتساب
        // الرصيد والتمديد والحفظ.
        var sponsor = await dbContext.Sponsors
            .FromSqlInterpolated(
                $@"SELECT *
               FROM ""Sponsors""
               WHERE ""Id"" = {sponsorId}
               FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "الكافل غير موجود.");

        if (!sponsor.IsActive)
        {
            throw new InvalidOperationException(
                "الكافل موقوف.");
        }

        var sponsorships = await dbContext.Sponsorships
            .Where(item =>
                item.SponsorId == sponsor.Id &&
                item.Status == SponsorshipStatus.Active)
            .OrderBy(item =>
                item.BeneficiaryId)
            .ToListAsync(cancellationToken);

        var calculation =
            SponsorshipPaymentCalculator
                .CalculateWithCredit(
                    sponsor.CreditBalance,
                    request.Amount,
                    sponsorships.Select(item =>
                        item.MonthlyAmount));

        var entry = CreateEntry(
            request,
            sponsor.Id,
            sponsor.Name);

        // تسجيل إيداع الدفعة في رصيد الكافل.
        entry.CreditTransactions.Add(
            new SponsorCreditTransaction
            {
                SponsorId = sponsor.Id,

                TransactionType =
                    SponsorCreditTransactionType.Deposit,

                Amount = request.Amount,

                BalanceAfter =
                    calculation.AvailableAmount,

                Notes =
                    "إضافة دفعة كفالة إلى رصيد الكافل."
            });

        // لا ننشئ توزيعات بقيمة صفر.
        if (calculation.AddedMonths > 0)
        {
            foreach (var sponsorship in sponsorships)
            {
                var previousEndDate =
                    sponsorship.EndDate;

                var allocatedAmount =
                    sponsorship.MonthlyAmount *
                    calculation.AddedMonths;

                sponsorship.ExtendByMonths(
                    calculation.AddedMonths);

                entry.Allocations.Add(
                    new PaymentAllocation
                    {
                        Sponsorship = sponsorship,

                        AllocatedAmount =
                            allocatedAmount,

                        AddedMonths =
                            calculation.AddedMonths,

                        PreviousEndDate =
                            previousEndDate,

                        NewEndDate =
                            sponsorship.EndDate
                    });
            }

            // تسجيل المبلغ الذي استُخدم لتمديد الكفالات.
            entry.CreditTransactions.Add(
                new SponsorCreditTransaction
                {
                    SponsorId = sponsor.Id,

                    TransactionType =
                        SponsorCreditTransactionType
                            .SponsorshipExtension,

                    Amount =
                        -calculation.AppliedAmount,

                    BalanceAfter =
                        calculation.RemainingBalance,

                    Notes =
                        $"تمديد الكفالات الفعالة " +
                        $"{calculation.AddedMonths} شهر."
                });
        }

        sponsor.CreditBalance =
            calculation.RemainingBalance;

        sponsor.UpdatedAtUtc =
            DateTime.UtcNow;

        dbContext.FinancialEntries.Add(entry);

        // الحركة المالية والإيداع واستخدام الرصيد
        // وتمديد الكفالات تحفظ معًا.
        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

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
                    item.ReceiptNumber == normalizedReceipt,
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