namespace GreenCrescent.Application.Features.Sponsors;

public sealed record UpdateSponsorRequest(
    int Id,
    string Name,
    string? PhoneNumber,
    string? Address);
