using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record OrphanApplicationFamilyMemberDto(
    int Id,
    string Name,
    DateOnly? DateOfBirth,
    PersonGender? Gender,
    string? Relationship,
    string? EducationalStatus,
    string? SocialStatus,
    string? HealthStatus,
    string? Occupation,
    decimal MonthlyIncome);