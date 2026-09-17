using GreenCrescent.Core.Common;

namespace GreenCrescent.Core.Entities
{
    public sealed class Sponsor : BaseEntity
    {
        public string Name { get; set; } =
            string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal CreditBalance { get; set; }

        public ICollection<Sponsorship> Sponsorships
        { get; set; } =
                new List<Sponsorship>();

        public ICollection<SponsorCreditTransaction>
            CreditTransactions
        { get; set; } =
                new List<SponsorCreditTransaction>();
    }
}