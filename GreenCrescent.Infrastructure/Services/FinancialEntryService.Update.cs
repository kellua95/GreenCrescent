using System.Data;
using System.Text.Json;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Core.Services;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GreenCrescent.Infrastructure.Services;

public sealed partial class FinancialEntryService
{
    public async Task UpdateAsync(
        UpdateFinancialEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureFinancialAccessAsync(cancellationToken);
        ValidateUpdateRequest(request);

        var userId = await currentUserService.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "يجب تسجيل الدخول.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var operationDb = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await using var transaction =
            await operationDb.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        // قراءة نسخة حديثة وقفل الحركة حتى انتهاء التعديل.
        var entry = await operationDb.FinancialEntries
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM "FinancialEntries"
                WHERE "Id" = {request.Id}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "الحركة المالية غير موجودة.");

        if (entry.RevisionNumber != request.ExpectedRevisionNumber)
        {
            throw new InvalidOperationException(
                "تم تعديل الحركة بواسطة مستخدم آخر. " +
                "أعد تحميلها قبل إجراء تعديل جديد.");
        }

        if (entry.Status == FinancialEntryStatus.Reversed)
        {
            throw new InvalidOperationException(
                "هذه الحركة معكوسة سابقًا ولا يمكن تعديلها بهذا المسار.");
        }

        if (entry.Status is not (
            FinancialEntryStatus.Confirmed or
            FinancialEntryStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "حالة الحركة المالية غير مدعومة.");
        }

        var bookNumber = request.BookNumber.Trim();
        var receiptNumber = request.ReceiptNumber.Trim();

        var duplicateReceipt =
            await operationDb.FinancialEntries.AnyAsync(
                item =>
                    item.Id != entry.Id &&
                    item.BookNumber == bookNumber &&
                    item.ReceiptNumber == receiptNumber,
                cancellationToken);

        if (duplicateReceipt)
        {
            throw new InvalidOperationException(
                "رقم الوصل مستخدم في حركة أخرى داخل هذا الدفتر.");
        }

        await operationDb.Entry(entry)
            .Collection(item => item.Allocations)
            .LoadAsync(cancellationToken);

        await operationDb.Entry(entry)
            .Collection(item => item.CreditTransactions)
            .LoadAsync(cancellationToken);

        var oldIsSponsorship =
            entry.Status == FinancialEntryStatus.Confirmed &&
            IsSponsorshipPayment(entry.EntryType);

        var newIsSponsorship =
            !request.IsCancelledReceipt &&
            IsSponsorshipPayment(request.EntryType);

        var recalculate =
            RequiresFinancialRecalculation(entry, request);

        int? newSponsorId = newIsSponsorship
            ? request.SponsorId
            : null;

        // ترتيب الأقفال ثابت عند النقل من كافل إلى آخر.
        var sponsorIds = new[]
        {
            entry.SponsorId,
            newSponsorId
        }
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct()
        .OrderBy(id => id)
        .ToArray();

        var sponsors = new Dictionary<int, Sponsor>();

        foreach (var sponsorId in sponsorIds)
        {
            var sponsor = await operationDb.Sponsors
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM "Sponsors"
                    WHERE "Id" = {sponsorId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "أحد الكفلاء المرتبطين بالحركة غير موجود.");

            sponsors.Add(sponsor.Id, sponsor);
        }

        if (recalculate && newIsSponsorship &&
            !sponsors[newSponsorId!.Value].IsActive)
        {
            throw new InvalidOperationException(
                "لا يمكن تطبيق دفعة كفالة جديدة على كافل موقوف.");
        }

        var sponsorships = new Dictionary<int, Sponsorship>();

        if (recalculate)
        {
            foreach (var sponsorId in sponsorIds)
            {
                var activeSponsorships =
                    await operationDb.Sponsorships
                        .FromSqlInterpolated(
                            $"""
                            SELECT *
                            FROM "Sponsorships"
                            WHERE "SponsorId" = {sponsorId}
                              AND "Status" = {(int)SponsorshipStatus.Active}
                            ORDER BY "Id"
                            FOR UPDATE
                            """)
                        .ToListAsync(cancellationToken);

                foreach (var sponsorship in activeSponsorships)
                {
                    sponsorships.TryAdd(
                        sponsorship.Id,
                        sponsorship);
                }
            }

            // تحميل الكفالات القديمة حتى لو لم تعد فعالة.
            foreach (var allocation in entry.Allocations
                         .OrderBy(item => item.SponsorshipId))
            {
                if (sponsorships.ContainsKey(
                        allocation.SponsorshipId))
                {
                    continue;
                }

                var sponsorshipId = allocation.SponsorshipId;

                var sponsorship = await operationDb.Sponsorships
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM "Sponsorships"
                        WHERE "Id" = {sponsorshipId}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new InvalidOperationException(
                        "إحدى الكفالات المرتبطة بالحركة غير موجودة.");

                sponsorships.Add(
                    sponsorship.Id,
                    sponsorship);
            }
        }

        if (recalculate && oldIsSponsorship)
        {
            await EnsureNoLaterCreditTransactionsAsync(
                operationDb,
                entry,
                cancellationToken);

            await EnsureOriginalSponsorshipEffectCanBeRemovedAsync(
                operationDb,
                entry,
                sponsorships,
                cancellationToken);
        }

        if (recalculate && !oldIsSponsorship)
        {
            var hasUnexpectedCredit =
                entry.CreditTransactions
                    .GroupBy(item => item.SponsorId)
                    .Any(group => group.Sum(item => item.Amount) != 0m);

            if (entry.Allocations.Count > 0 || hasUnexpectedCredit)
            {
                throw new InvalidOperationException(
                    "توجد آثار كفالات أو رصيد لا تتوافق مع نوع " +
                    "الحركة الحالي. يجب مراجعتها قبل تعديلها.");
            }
        }

        var beforeJson = BuildFinancialEntrySnapshot(
            entry,
            sponsors.Values,
            sponsorships.Values);

        if (recalculate && oldIsSponsorship)
        {
            RemoveOriginalSponsorshipEffect(
                operationDb,
                entry,
                sponsors[entry.SponsorId!.Value],
                sponsorships);

            // حذف التوزيعات القديمة قبل إنشاء البديلة،
            // مع بقاء كل شيء داخل المعاملة نفسها.
            await operationDb.SaveChangesAsync(cancellationToken);
        }

        entry.SponsorId = newSponsorId;

        entry.DonorName = request.IsCancelledReceipt
            ? "وصل ملغي"
            : newIsSponsorship
                ? sponsors[newSponsorId!.Value].Name
                : request.DonorName!.Trim();

        entry.EntryType = request.IsCancelledReceipt
            ? FinancialEntryType.Other
            : request.EntryType;

        entry.PaymentMethod = request.IsCancelledReceipt
            ? PaymentMethod.Cash
            : request.PaymentMethod;

        entry.Amount = request.IsCancelledReceipt
            ? 0m
            : request.Amount;

        entry.BookNumber = bookNumber;
        entry.ReceiptNumber = receiptNumber;
        entry.EntryDate = request.EntryDate;
        entry.Notes = NormalizeOptional(request.Notes);

        entry.Status = request.IsCancelledReceipt
            ? FinancialEntryStatus.Cancelled
            : FinancialEntryStatus.Confirmed;

        entry.RevisionNumber =
            checked(entry.RevisionNumber + 1);

        entry.UpdatedAtUtc = DateTime.UtcNow;

        if (recalculate && newIsSponsorship)
        {
            ApplyCorrectedSponsorshipEffect(
                entry,
                sponsors[newSponsorId!.Value],
                sponsorships.Values
                    .Where(item =>
                        item.SponsorId == newSponsorId.Value &&
                        item.Status == SponsorshipStatus.Active)
                    .OrderBy(item => item.Id)
                    .ToList());
        }

        await operationDb.SaveChangesAsync(cancellationToken);

        var afterJson = BuildFinancialEntrySnapshot(
            entry,
            sponsors.Values,
            sponsorships.Values);

        operationDb.Set<FinancialEntryRevision>().Add(
            new FinancialEntryRevision
            {
                FinancialEntryId = entry.Id,
                Reason = request.ChangeReason.Trim(),
                PerformedByUserId = userId,
                BeforeJson = beforeJson,
                AfterJson = afterJson
            });

        await operationDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task
        EnsureOriginalSponsorshipEffectCanBeRemovedAsync(
            ApplicationDbContext operationDb,
            FinancialEntry entry,
            IReadOnlyDictionary<int, Sponsorship> sponsorships,
            CancellationToken cancellationToken)
    {
        var sponsorId = entry.SponsorId!.Value;

        var linkedCredit = entry.CreditTransactions
            .Where(item => item.SponsorId == sponsorId)
            .ToList();

        if (linkedCredit.Count == 0)
        {
            throw new InvalidOperationException(
                "لا يوجد سجل رصيد مرتبط بهذه الدفعة.");
        }

        var expectedNet =
            entry.Amount -
            entry.Allocations.Sum(item => item.AllocatedAmount);

        if (linkedCredit.Sum(item => item.Amount) != expectedNet)
        {
            throw new InvalidOperationException(
                "سجل رصيد الحركة لا يتطابق مع توزيعاتها. " +
                "يجب مراجعتها قبل تعديلها.");
        }

        var hasOtherOutstandingCredit =
            entry.CreditTransactions
                .Where(item => item.SponsorId != sponsorId)
                .GroupBy(item => item.SponsorId)
                .Any(group => group.Sum(item => item.Amount) != 0m);

        if (hasOtherOutstandingCredit)
        {
            throw new InvalidOperationException(
                "توجد آثار رصيد مرتبطة بكافل آخر تحتاج مراجعة.");
        }

        var effectRecordedAt =
            linkedCredit.Max(item => item.CreatedAtUtc);

        var activeSponsorships = sponsorships.Values
            .Where(item =>
                item.SponsorId == sponsorId &&
                item.Status == SponsorshipStatus.Active)
            .ToList();

        // لا نعيد توزيع دفعة قديمة على مجموعة كفالات
        // أُنشئت أو تغيرت بعد تسجيل أثرها.
        if (activeSponsorships.Any(item =>
                item.CreatedAtUtc > effectRecordedAt ||
                (item.UpdatedAtUtc.HasValue &&
                 item.UpdatedAtUtc.Value > effectRecordedAt)))
        {
            throw new InvalidOperationException(
                "تغيرت بيانات كفالات هذا الكافل بعد تسجيل " +
                "أثر الدفعة. يلزم مراجعة أثر التعديل أولًا.");
        }

        var affectedIds = sponsorships.Values
            .Where(item => item.SponsorId == sponsorId)
            .Select(item => item.Id)
            .Concat(entry.Allocations.Select(item => item.SponsorshipId))
            .Distinct()
            .ToArray();

        var hasLaterSponsorshipChanges =
            await operationDb.SponsorshipChanges.AnyAsync(
                item =>
                    item.CreatedAtUtc > effectRecordedAt &&
                    (
                        item.OldSponsorId == sponsorId ||
                        item.NewSponsorId == sponsorId ||
                        affectedIds.Contains(item.OldSponsorshipId) ||
                        (
                            item.NewSponsorshipId.HasValue &&
                            affectedIds.Contains(
                                item.NewSponsorshipId.Value)
                        )
                    ),
                cancellationToken);

        if (hasLaterSponsorshipChanges)
        {
            throw new InvalidOperationException(
                "حدث استبدال أو إجراء على الكفالات بعد هذه الدفعة. " +
                "لا يمكن إعادة حسابها تلقائيًا بهذا المسار.");
        }

        if (entry.Allocations.Count > 0)
        {
            var originalIds = entry.Allocations
                .Select(item => item.SponsorshipId)
                .OrderBy(id => id)
                .ToArray();

            var currentIds = activeSponsorships
                .Select(item => item.Id)
                .OrderBy(id => id)
                .ToArray();

            if (!originalIds.SequenceEqual(currentIds))
            {
                throw new InvalidOperationException(
                    "مجموعة الكفالات الفعالة تغيرت منذ تسجيل الدفعة. " +
                    "يجب مراجعتها قبل إعادة توزيع المبلغ.");
            }
        }

        foreach (var allocation in entry.Allocations)
        {
            var sponsorship =
                sponsorships[allocation.SponsorshipId];

            if (sponsorship.SponsorId != sponsorId ||
                sponsorship.Status != SponsorshipStatus.Active ||
                sponsorship.EndDate != allocation.NewEndDate ||
                allocation.PreviousEndDate < sponsorship.StartDate ||
                allocation.AddedMonths <= 0 ||
                allocation.AllocatedAmount !=
                    sponsorship.MonthlyAmount * allocation.AddedMonths ||
                allocation.PreviousEndDate.AddMonths(
                    allocation.AddedMonths) != allocation.NewEndDate)
            {
                throw new InvalidOperationException(
                    "بيانات إحدى الكفالات لم تعد مطابقة " +
                    "لتوزيع الدفعة الأصلية. لم يتم تعديل الحركة.");
            }

            var hasLaterAllocation =
                await operationDb.PaymentAllocations.AnyAsync(
                    item =>
                        item.SponsorshipId == allocation.SponsorshipId &&
                        item.FinancialEntryId != entry.Id &&
                        item.Id > allocation.Id,
                    cancellationToken);

            if (hasLaterAllocation)
            {
                throw new InvalidOperationException(
                    "توجد توزيعات لاحقة على إحدى الكفالات. " +
                    "يجب معالجة ترابطها قبل تعديل هذه الدفعة.");
            }
        }
    }

    private static void RemoveOriginalSponsorshipEffect(
        ApplicationDbContext operationDb,
        FinancialEntry entry,
        Sponsor sponsor,
        IReadOnlyDictionary<int, Sponsorship> sponsorships)
    {
        var oldNet = entry.CreditTransactions
            .Where(item => item.SponsorId == sponsor.Id)
            .Sum(item => item.Amount);

        var restoredBalance = sponsor.CreditBalance - oldNet;

        if (restoredBalance < 0m)
        {
            throw new InvalidOperationException(
                "إلغاء أثر الدفعة سيجعل رصيد الكافل سالبًا. " +
                "يجب مراجعة الحركات المرتبطة.");
        }

        foreach (var allocation in entry.Allocations)
        {
            var sponsorship =
                sponsorships[allocation.SponsorshipId];

            // استعادة التاريخ المحفوظ، وليس طرح الأشهر؛
            // لأن أطوال الأشهر مختلفة.
            sponsorship.EndDate = allocation.PreviousEndDate;
            sponsorship.UpdatedAtUtc = DateTime.UtcNow;
        }

        sponsor.CreditBalance = restoredBalance;
        sponsor.UpdatedAtUtc = DateTime.UtcNow;

        if (oldNet != 0m)
        {
            entry.CreditTransactions.Add(
                new SponsorCreditTransaction
                {
                    SponsorId = sponsor.Id,
                    TransactionType =
                        SponsorCreditTransactionType.Reversal,
                    Amount = -oldNet,
                    BalanceAfter = restoredBalance,
                    Notes =
                        $"إلغاء أثر النسخة {entry.RevisionNumber} " +
                        "تمهيدًا لتصحيح الحركة."
                });
        }

        var oldAllocations = entry.Allocations.ToList();

        operationDb.PaymentAllocations.RemoveRange(oldAllocations);
        entry.Allocations.Clear();
    }

    private static void ApplyCorrectedSponsorshipEffect(
        FinancialEntry entry,
        Sponsor sponsor,
        IReadOnlyList<Sponsorship> sponsorships)
    {
        var calculation =
            SponsorshipPaymentCalculator.CalculateWithCredit(
                sponsor.CreditBalance,
                entry.Amount,
                sponsorships.Select(item => item.MonthlyAmount));

        entry.CreditTransactions.Add(
            new SponsorCreditTransaction
            {
                SponsorId = sponsor.Id,
                TransactionType =
                    SponsorCreditTransactionType.Deposit,
                Amount = entry.Amount,
                BalanceAfter = calculation.AvailableAmount,
                Notes =
                    $"تطبيق مبلغ الحركة بعد التعديل، " +
                    $"النسخة {entry.RevisionNumber}."
            });

        if (calculation.AddedMonths > 0)
        {
            foreach (var sponsorship in sponsorships)
            {
                var previousEndDate = sponsorship.EndDate;

                sponsorship.ExtendByMonths(calculation.AddedMonths);

                entry.Allocations.Add(
                    new PaymentAllocation
                    {
                        SponsorshipId = sponsorship.Id,
                        AllocatedAmount =
                            sponsorship.MonthlyAmount *
                            calculation.AddedMonths,
                        AddedMonths = calculation.AddedMonths,
                        PreviousEndDate = previousEndDate,
                        NewEndDate = sponsorship.EndDate
                    });
            }

            entry.CreditTransactions.Add(
                new SponsorCreditTransaction
                {
                    SponsorId = sponsor.Id,
                    TransactionType =
                        SponsorCreditTransactionType.SponsorshipExtension,
                    Amount = -calculation.AppliedAmount,
                    BalanceAfter = calculation.RemainingBalance,
                    Notes =
                        $"تمديد الكفالات بعد تعديل الحركة " +
                        $"{calculation.AddedMonths} شهر."
                });
        }

        sponsor.CreditBalance = calculation.RemainingBalance;
        sponsor.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string BuildFinancialEntrySnapshot(
        FinancialEntry entry,
        IEnumerable<Sponsor> sponsors,
        IEnumerable<Sponsorship> sponsorships)
    {
        return JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,

            Entry = new
            {
                entry.Id,
                entry.RevisionNumber,
                entry.SponsorId,
                entry.DonorName,
                entry.EntryType,
                entry.Amount,
                entry.PaymentMethod,
                entry.BookNumber,
                entry.ReceiptNumber,
                entry.EntryDate,
                entry.Status,
                entry.Notes,
                entry.ReversedAtUtc,
                entry.ReversalReason,
                entry.CreatedAtUtc,
                entry.UpdatedAtUtc
            },

            Allocations = entry.Allocations
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.SponsorshipId,
                    item.AllocatedAmount,
                    item.AddedMonths,
                    item.PreviousEndDate,
                    item.NewEndDate
                })
                .ToArray(),

            CreditTransactions = entry.CreditTransactions
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.SponsorId,
                    item.TransactionType,
                    item.Amount,
                    item.BalanceAfter,
                    item.Notes,
                    item.CreatedAtUtc
                })
                .ToArray(),

            SponsorBalances = sponsors
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.CreditBalance
                })
                .ToArray(),

            Sponsorships = sponsorships
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.SponsorId,
                    item.BeneficiaryId,
                    item.MonthlyAmount,
                    item.StartDate,
                    item.EndDate,
                    item.Status
                })
                .ToArray()
        });
    }
}