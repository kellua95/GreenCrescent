using GreenCrescent.Core.Common;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Core.Entities;

public sealed class OrphanApplicationFamilyMember
    : BaseEntity
{
    public int OrphanApplicationId { get; set; }

    public OrphanApplication OrphanApplication { get; set; } =
        null!;

    public string Name { get; set; } =
        string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public PersonGender? Gender { get; set; }

    public string? Relationship { get; set; }

    public string? EducationalStatus { get; set; }

    public string? SocialStatus { get; set; }

    public string? HealthStatus { get; set; }

    public string? Occupation { get; set; }

    public decimal MonthlyIncome { get; set; }
}