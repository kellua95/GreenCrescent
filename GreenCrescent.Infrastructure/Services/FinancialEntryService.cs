using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Core.Services;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using GreenCrescent.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GreenCrescent.Infrastructure.Services;

public sealed partial class FinancialEntryService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IServiceScopeFactory scopeFactory)
    : IFinancialEntryService
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

    public async Task<int> CreateCancelledReceiptAsync(
    CreateCancelledReceiptRequest request,
    CancellationToken cancellationToken = default)
    {
        await EnsureFinancialAccessAsync(cancellationToken);

        var bookNumber = NormalizeOptional(request.BookNumber);
        var receiptNumber = NormalizeOptional(request.ReceiptNumber);
        var reason = NormalizeOptional(request.Reason);

        if (bookNumber is null || receiptNumber is null)
        {
            throw new InvalidOperationException(
                "رقم الدفتر ورقم الوصل مطلوبان.");
        }

        if (bookNumber.Length > 50 || receiptNumber.Length > 50)
        {
            throw new InvalidOperationException(
                "رقم الدفتر ورقم الوصل يجب ألا يتجاوز كل منهما 50 حرفًا.");
        }

        if (reason is null)
        {
            throw new InvalidOperationException(
                "سبب إلغاء الوصل مطلوب.");
        }

        if (reason.Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الإلغاء يجب ألا يتجاوز 1000 حرف.");
        }

        if (request.EntryDate == DateOnly.MinValue)
        {
            throw new InvalidOperationException(
                "تاريخ الوصل مطلوب.");
        }

        await EnsureReceiptIsNotDuplicatedAsync(
            bookNumber,
            receiptNumber,
            cancellationToken);

        var entry = new FinancialEntry
        {
            SponsorId = null,
            DonorName = "وصل ملغي",
            EntryType = FinancialEntryType.Other,

            // الحقل إلزامي في النموذج الحالي.
            // لا تمثل هذه القيمة قبضًا نقديًا للوصل الملغي.
            PaymentMethod = PaymentMethod.Cash,

            Amount = 0m,
            BookNumber = bookNumber,
            ReceiptNumber = receiptNumber,
            EntryDate = request.EntryDate,
            Status = FinancialEntryStatus.Cancelled,
            Notes = reason
        };

        dbContext.FinancialEntries.Add(entry);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return entry.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgres.ConstraintName ==
                      "IX_FinancialEntries_Book_Receipt_Unique")
        {
            dbContext.Entry(entry).State = EntityState.Detached;

            throw new InvalidOperationException(
                "رقم الوصل مستخدم مسبقًا في هذا الدفتر.",
                exception);
        }
        catch
        {
            // لا نترك الإدخال الفاشل معلّقًا ليُحفظ مع عملية لاحقة.
            dbContext.Entry(entry).State = EntityState.Detached;
            throw;
        }
    }

    private async Task EnsureFinancialAccessAsync(
    CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "يجب تسجيل الدخول.");
        }

        var allowed = await (
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles
                on user.Id equals userRole.UserId
            join role in dbContext.Roles
                on userRole.RoleId equals role.Id
            where user.Id == userId
                  && user.IsActive
                  && (
                      role.Name == AppRoles.Admin ||
                      role.Name == AppRoles.Staff
                  )
            select user.Id
        ).AnyAsync(cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                "ليس لديك صلاحية تسجيل الحركات المالية.");
        }
    }

    public async Task<int> CreateAsync(
        CreateFinancialEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureFinancialAccessAsync(cancellationToken);

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
                item.Notes)
            {
                RevisionNumber = item.RevisionNumber
            })
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

        var donorName = request.DonorName.Trim();

        if (donorName.Length > 200)
        {
            throw new InvalidOperationException(
                "اسم المتبرع يجب ألا يتجاوز 200 حرف.");
        }

        var entry = CreateEntry(
            request,
            sponsorId: null,
            donorName: donorName);

        dbContext.FinancialEntries.Add(entry);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return entry.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgres.ConstraintName ==
                      "IX_FinancialEntries_Book_Receipt_Unique")
        {
            dbContext.Entry(entry).State = EntityState.Detached;

            throw new InvalidOperationException(
                "رقم الوصل مستخدم مسبقًا في هذا الدفتر.",
                exception);
        }
        catch
        {
            dbContext.Entry(entry).State = EntityState.Detached;
            throw;
        }
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

        await using var paymentScope = scopeFactory.CreateAsyncScope();

        var paymentDbContext = paymentScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await using var transaction =
            await paymentDbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var sponsorId =
            request.SponsorId.Value;

        // قفل سجل الكافل حتى تنتهي عملية احتساب
        // الرصيد والتمديد والحفظ.
        var sponsor = await paymentDbContext.Sponsors
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

        var sponsorships = await paymentDbContext.Sponsorships
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

        paymentDbContext.FinancialEntries.Add(entry);

        // الحركة المالية والإيداع واستخدام الرصيد
        // وتمديد الكفالات تحفظ معًا.
        await paymentDbContext.SaveChangesAsync(
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
        if (string.IsNullOrWhiteSpace(request.BookNumber))
        {
            throw new InvalidOperationException(
                "رقم الدفتر مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(request.ReceiptNumber))
        {
            throw new InvalidOperationException(
                "رقم الوصل مطلوب.");
        }

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

    public async Task<FinancialEntryDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        await EnsureFinancialAccessAsync(cancellationToken);

        if (id <= 0)
        {
            throw new InvalidOperationException(
                "رقم الحركة المالية غير صحيح.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var readDbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await readDbContext.FinancialEntries
            .AsNoTracking()
            .Where(item => item.Id == id)
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
                item.Notes)
            {
                RevisionNumber = item.RevisionNumber
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<FinancialEntryDto?> GetByReceiptAsync(
    string bookNumber,
    string receiptNumber,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookNumber) ||
            string.IsNullOrWhiteSpace(receiptNumber))
        {
            throw new InvalidOperationException(
                "أدخل رقم الدفتر ورقم الوصل للتحقق.");
        }

        var book = bookNumber.Trim();
        var receipt = receiptNumber.Trim();

        return await dbContext.FinancialEntries
            .AsNoTracking()
            .Where(item =>
                item.BookNumber == book &&
                item.ReceiptNumber == receipt)
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
                item.Notes)
            {
                RevisionNumber = item.RevisionNumber
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static void ValidateUpdateRequest(
    UpdateFinancialEntryRequest request)
    {
        if (request.Id <= 0)
        {
            throw new InvalidOperationException(
                "رقم الحركة المالية غير صحيح.");
        }

        if (request.ExpectedRevisionNumber < 0)
        {
            throw new InvalidOperationException(
                "رقم نسخة الحركة غير صحيح.");
        }

        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            throw new InvalidOperationException(
                "سبب تعديل الحركة مطلوب.");
        }

        if (request.ChangeReason.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب التعديل يجب ألا يتجاوز 1000 حرف.");
        }

        if (string.IsNullOrWhiteSpace(request.BookNumber) ||
            string.IsNullOrWhiteSpace(request.ReceiptNumber))
        {
            throw new InvalidOperationException(
                "رقم الدفتر ورقم الوصل مطلوبان.");
        }

        if (request.BookNumber.Trim().Length > 50 ||
            request.ReceiptNumber.Trim().Length > 50)
        {
            throw new InvalidOperationException(
                "رقم الدفتر ورقم الوصل يجب ألا يتجاوز كل منهما 50 حرفًا.");
        }

        if (request.EntryDate == DateOnly.MinValue)
        {
            throw new InvalidOperationException(
                "تاريخ الحركة مطلوب.");
        }

        if (request.Notes?.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }

        if (request.IsCancelledReceipt)
        {
            if (request.Amount != 0m)
            {
                throw new InvalidOperationException(
                    "قيمة الوصل الملغي يجب أن تكون صفرًا.");
            }

            if (string.IsNullOrWhiteSpace(request.Notes))
            {
                throw new InvalidOperationException(
                    "سبب إلغاء الوصل مطلوب في الملاحظات.");
            }

            return;
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

        if (request.Amount < 0.001m ||
            request.Amount > 999999999999m)
        {
            throw new InvalidOperationException(
                "القيمة يجب أن تكون بين 0.001 و999999999999.");
        }

        if (decimal.Round(request.Amount, 3) != request.Amount)
        {
            throw new InvalidOperationException(
                "القيمة المالية يجب ألا تتجاوز ثلاث منازل عشرية.");
        }

        if (IsSponsorshipPayment(request.EntryType))
        {
            if (!request.SponsorId.HasValue ||
                request.SponsorId.Value <= 0)
            {
                throw new InvalidOperationException(
                    "يجب اختيار الكافل من السجلات.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.DonorName))
            {
                throw new InvalidOperationException(
                    "اسم المتبرع مطلوب.");
            }

            if (request.DonorName.Trim().Length > 200)
            {
                throw new InvalidOperationException(
                    "اسم المتبرع يجب ألا يتجاوز 200 حرف.");
            }
        }
    }

    private static bool RequiresFinancialRecalculation(
    FinancialEntry entry,
    UpdateFinancialEntryRequest request)
    {
        var oldIsSponsorship =
            entry.Status == FinancialEntryStatus.Confirmed &&
            IsSponsorshipPayment(entry.EntryType);

        var newIsSponsorship =
            !request.IsCancelledReceipt &&
            IsSponsorshipPayment(request.EntryType);

        if (oldIsSponsorship != newIsSponsorship)
        {
            return true;
        }

        if (!oldIsSponsorship)
        {
            // التبرعات والوصولات الملغية لا تمدد الكفالات
            // ولا تدخل في رصيد الكافل.
            return false;
        }

        return entry.SponsorId != request.SponsorId ||
               entry.Amount != request.Amount;
    }

    private static async Task EnsureNoLaterCreditTransactionsAsync(
    ApplicationDbContext operationDbContext,
    FinancialEntry entry,
    CancellationToken cancellationToken)
    {
        if (entry.Status != FinancialEntryStatus.Confirmed ||
            !IsSponsorshipPayment(entry.EntryType))
        {
            return;
        }

        if (!entry.SponsorId.HasValue)
        {
            throw new InvalidOperationException(
                "هذه الحركة مسجلة كدفعة كفالة دون كافل مرتبط. " +
                "يجب مراجعة بياناتها قبل تعديل أثرها المالي.");
        }

        var sponsorId = entry.SponsorId.Value;

        var lastLinkedTransactionId =
            await operationDbContext
                .Set<SponsorCreditTransaction>()
                .Where(item =>
                    item.FinancialEntryId == entry.Id &&
                    item.SponsorId == sponsorId)
                .Select(item => (int?)item.Id)
                .MaxAsync(cancellationToken);

        if (!lastLinkedTransactionId.HasValue)
        {
            throw new InvalidOperationException(
                "لا يوجد سجل رصيد مرتبط بهذه الدفعة يسمح " +
                "بإعادة حساب أثرها تلقائيًا. يجب مراجعتها أولًا.");
        }

        var hasLaterTransactions =
            await operationDbContext
                .Set<SponsorCreditTransaction>()
                .AnyAsync(
                    item =>
                        item.SponsorId == sponsorId &&
                        item.Id > lastLinkedTransactionId.Value &&
                        (
                            item.FinancialEntryId == null ||
                            item.FinancialEntryId != entry.Id
                        ),
                    cancellationToken);

        if (hasLaterTransactions)
        {
            throw new InvalidOperationException(
                "توجد حركات رصيد لاحقة لهذا الكافل. " +
                "تعديل مبلغ هذه الدفعة أو كافلها أو تحويلها " +
                "إلى تبرع يحتاج إعادة حساب الحركات المرتبطة؛ " +
                "لم يتم تغيير أي بيانات.");
        }
    }
}