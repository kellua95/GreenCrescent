using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities
{
    public sealed class Beneficiary : BaseEntity
    {
        public int FileNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public bool IsArchived { get; set; }

        public DateTime? ArchivedAtUtc { get; set; }

        public string? ArchiveReason { get; set; }

        public BeneficiaryStatus Status { get; set; } =
            BeneficiaryStatus.WaitingForSponsor;

        public string? Notes { get; set; }

        public ICollection<Sponsorship> Sponsorships { get; set; } =
            new List<Sponsorship>();
    }
}
