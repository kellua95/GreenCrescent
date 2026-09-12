using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities
{
    public sealed class Sponsorship : BaseEntity
    {
        public int SponsorId { get; set; }

        public Sponsor Sponsor { get; set; } = null!;

        public int BeneficiaryId { get; set; }

        public Beneficiary Beneficiary { get; set; } = null!;

        public decimal MonthlyAmount { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public SponsorshipStatus Status { get; set; } =
            SponsorshipStatus.Active;

        public string? Notes { get; set; }

        public int? ResponsibleSheikhId { get; set; }

        public ResponsibleSheikh? ResponsibleSheikh { get; set; }

        public void ExtendByMonths(int numberOfMonths)
        {
            if (numberOfMonths <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(numberOfMonths),
                    "يجب أن يكون عدد الأشهر أكبر من صفر.");
            }

            EndDate = EndDate.AddMonths(numberOfMonths);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
