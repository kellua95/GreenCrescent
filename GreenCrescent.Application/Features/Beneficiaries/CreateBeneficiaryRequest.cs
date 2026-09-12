namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed record CreateBeneficiaryRequest(
    int FileNumber,
    string Name,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Notes);