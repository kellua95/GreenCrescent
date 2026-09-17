using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities;

public sealed class SponsorCreditTransaction : BaseEntity
{
    public int SponsorId { get; set; }

    public Sponsor Sponsor { get; set; } = null!;

    public int? FinancialEntryId { get; set; }

    public FinancialEntry? FinancialEntry { get; set; }

    public SponsorCreditTransactionType TransactionType
    { get; set; }

    // الإيداع موجب، واستخدام الرصيد سالب.
    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public string? Notes { get; set; }
}