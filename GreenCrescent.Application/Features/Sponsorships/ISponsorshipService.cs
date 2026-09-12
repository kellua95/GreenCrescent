namespace GreenCrescent.Application.Features.Sponsorships;

public interface ISponsorshipService
{
    Task<IReadOnlyList<SponsorshipDto>> SearchAsync(
        string? searchTerm,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<SponsorshipDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        CreateSponsorshipRequest request,
        CancellationToken cancellationToken = default);

    Task StopAsync(
        int sponsorshipId,
        string reason,
        CancellationToken cancellationToken = default);
}