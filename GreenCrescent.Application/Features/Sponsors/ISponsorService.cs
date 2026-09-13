using GreenCrescent.Application.Common.Models;

namespace GreenCrescent.Application.Features.Sponsors
{
    public interface ISponsorService
    {
        Task<IReadOnlyList<SponsorDto>> SearchAsync(
        string? searchTerm,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

        Task<SponsorDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<PagedResult<SponsorDto>> SearchPageAsync(
            string? searchTerm,
            bool includeInactive,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<int> CreateAsync(
            CreateSponsorRequest request,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            UpdateSponsorRequest request,
            CancellationToken cancellationToken = default);

        Task SetActiveAsync(
            int id,
            bool isActive,
            CancellationToken cancellationToken = default);
    }
}
