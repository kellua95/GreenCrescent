namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed record CreateBeneficiaryRequest(
    int FileNumber,
    string Name,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Notes)
{
    public BeneficiaryAdditionalData? AdditionalData { get; init; }

    public byte[]? PhotoData { get; init; }

    public string? PhotoContentType { get; init; }
}