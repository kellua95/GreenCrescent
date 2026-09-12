namespace GreenCrescent.Application.Features.Sponsors;

public sealed record CreateSponsorRequest(
    string Name,
    string? PhoneNumber,
    string? Address
    );