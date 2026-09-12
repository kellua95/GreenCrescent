namespace GreenCrescent.Core.Services
{
    public static class SponsorshipPaymentCalculator
    {
        public static int CalculateMonths(
        decimal paidAmount,
        IEnumerable<decimal> monthlyAmounts)
        {
            if (paidAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(paidAmount),
                    "يجب أن تكون قيمة الدفعة أكبر من صفر.");
            }

            var amounts = monthlyAmounts.ToList();

            if (amounts.Count == 0)
            {
                throw new InvalidOperationException(
                    "لا توجد كفالات فعالة لهذا الكافل.");
            }

            if (amounts.Any(amount => amount <= 0))
            {
                throw new InvalidOperationException(
                    "جميع قيم الكفالات الشهرية يجب أن تكون أكبر من صفر.");
            }

            var totalMonthlyAmount = amounts.Sum();

            if (paidAmount % totalMonthlyAmount != 0)
            {
                throw new InvalidOperationException(
                    $"يجب أن تكون الدفعة من مضاعفات مجموع الكفالات الشهرية وهو {totalMonthlyAmount}.");
            }

            var months = paidAmount / totalMonthlyAmount;

            if (months > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "عدد الأشهر الناتج أكبر من الحد المسموح.");
            }

            return decimal.ToInt32(months);
        }
    }
}
