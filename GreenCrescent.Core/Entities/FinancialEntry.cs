using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities
{
    public sealed class FinancialEntry : BaseEntity
    {
        public int? SponsorId { get; set; }

        public Sponsor? Sponsor { get; set; }

        public string DonorName { get; set; } = string.Empty;

        public FinancialEntryType EntryType { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string? BookNumber { get; set; }

        public string? ReceiptNumber { get; set; }

        public DateOnly EntryDate { get; set; }

        public string? Notes { get; set; }

        public FinancialEntryStatus Status { get; set; } =
            FinancialEntryStatus.Confirmed;

        public DateTime? ReversedAtUtc { get; set; }

        public string? ReversalReason { get; set; }

        public ICollection<PaymentAllocation> Allocations { get; set; } =
            new List<PaymentAllocation>();
    }
}
