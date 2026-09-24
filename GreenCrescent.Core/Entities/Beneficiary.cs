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

        public string? NationalNumber { get; set; }

        public PersonGender? Gender { get; set; }

        public string? Nationality { get; set; }

        public string? Address { get; set; }

        public byte[]? PhotoData { get; set; }

        public string? PhotoContentType { get; set; }

        public string? GuardianName { get; set; }

        public string? GuardianNationalNumber { get; set; }

        public string? GuardianPhoneNumber { get; set; }

        public string? GuardianRelationship { get; set; }

        public DateOnly? FatherDeathDate { get; set; }

        public string? FatherDeathReason { get; set; }

        public int? FamilyMembersCount { get; set; }

        public decimal? TotalMonthlyIncome { get; set; }

        public BeneficiaryStatus Status { get; set; } =
            BeneficiaryStatus.WaitingForSponsor;

        public string? Notes { get; set; }

        public ICollection<Sponsorship> Sponsorships { get; set; } =
            new List<Sponsorship>();
    }
}
