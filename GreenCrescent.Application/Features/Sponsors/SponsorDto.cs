namespace GreenCrescent.Application.Features.Sponsors
{
    public sealed record SponsorDto
    (
        int Id,
        string Name,
        string? PhoneNumber,
        string? Address,
        bool IsActive,
        int ActiveSponsorshipsCount,
        decimal CreditBalance);
}
