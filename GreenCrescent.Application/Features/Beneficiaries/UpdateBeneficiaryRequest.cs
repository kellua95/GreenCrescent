namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed record UpdateBeneficiaryRequest(
    int Id,
    int FileNumber,
    string Name,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Notes);