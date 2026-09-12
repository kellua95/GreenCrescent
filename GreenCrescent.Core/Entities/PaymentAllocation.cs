using GreenCrescent.Core.Common;

namespace GreenCrescent.Core.Entities
{
    public sealed class PaymentAllocation : BaseEntity
    {
        public int FinancialEntryId { get; set; }

        public FinancialEntry FinancialEntry { get; set; } = null!;

        public int SponsorshipId { get; set; }

        public Sponsorship Sponsorship { get; set; } = null!;

        public decimal AllocatedAmount { get; set; }

        public int AddedMonths { get; set; }

        public DateOnly PreviousEndDate { get; set; }

        public DateOnly NewEndDate { get; set; }
    }
}
