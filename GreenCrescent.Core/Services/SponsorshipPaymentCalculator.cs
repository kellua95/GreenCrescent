namespace GreenCrescent.Core.Services;

public sealed record SponsorshipCreditCalculation(
    decimal PreviousBalance,
    decimal PaidAmount,
    decimal AvailableAmount,
    decimal TotalMonthlyAmount,
    int AddedMonths,
    decimal AppliedAmount,
    decimal RemainingBalance);

public static class SponsorshipPaymentCalculator
{
    public static SponsorshipCreditCalculation
        CalculateWithCredit(
            decimal previousBalance,
            decimal paidAmount,
            IEnumerable<decimal> monthlyAmounts)
    {
        if (previousBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(previousBalance),
                "لا يمكن أن يكون الرصيد السابق سالبًا.");
        }

        if (paidAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paidAmount),
                "يجب أن تكون قيمة الدفعة أكبر من صفر.");
        }

        var amounts = monthlyAmounts.ToList();

        if (amounts.Any(amount => amount <= 0))
        {
            throw new InvalidOperationException(
                "جميع قيم الكفالات الشهرية يجب أن تكون أكبر من صفر.");
        }

        var availableAmount =
            previousBalance + paidAmount;

        // إذا لم توجد كفالات فعالة، تُحفظ الدفعة
        // كاملة في رصيد الكافل.
        if (amounts.Count == 0)
        {
            return new SponsorshipCreditCalculation(
                previousBalance,
                paidAmount,
                availableAmount,
                TotalMonthlyAmount: 0m,
                AddedMonths: 0,
                AppliedAmount: 0m,
                RemainingBalance: availableAmount);
        }

        var totalMonthlyAmount =
            amounts.Sum();

        var monthsDecimal =
            decimal.Floor(
                availableAmount /
                totalMonthlyAmount);

        if (monthsDecimal > int.MaxValue)
        {
            throw new InvalidOperationException(
                "عدد الأشهر الناتج أكبر من الحد المسموح.");
        }

        var addedMonths =
            decimal.ToInt32(monthsDecimal);

        var appliedAmount =
            totalMonthlyAmount * addedMonths;

        var remainingBalance =
            availableAmount - appliedAmount;

        return new SponsorshipCreditCalculation(
            previousBalance,
            paidAmount,
            availableAmount,
            totalMonthlyAmount,
            addedMonths,
            appliedAmount,
            remainingBalance);
    }

    // نبقي الدالة القديمة مؤقتًا حتى يظل المشروع
    // يعمل قبل تعديل FinancialEntryService.
    public static int CalculateMonths(
        decimal paidAmount,
        IEnumerable<decimal> monthlyAmounts)
    {
        var calculation =
            CalculateWithCredit(
                previousBalance: 0m,
                paidAmount,
                monthlyAmounts);

        if (calculation.TotalMonthlyAmount <= 0)
        {
            throw new InvalidOperationException(
                "لا توجد كفالات فعالة لهذا الكافل.");
        }

        if (calculation.RemainingBalance != 0)
        {
            throw new InvalidOperationException(
                $"يجب أن تكون الدفعة من مضاعفات مجموع الكفالات الشهرية وهو {calculation.TotalMonthlyAmount}.");
        }

        return calculation.AddedMonths;
    }
}