using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities;

public sealed class SponsorshipChange : BaseEntity
{
    public SponsorshipChangeType ChangeType { get; set; }

    public int OldSponsorshipId { get; set; }

    public int? NewSponsorshipId { get; set; }

    public int? OldSponsorId { get; set; }

    public int? NewSponsorId { get; set; }

    public int? OldBeneficiaryId { get; set; }

    public int? NewBeneficiaryId { get; set; }

    public decimal? OldMonthlyAmount { get; set; }

    public decimal? NewMonthlyAmount { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? PerformedByUserId { get; set; }
}