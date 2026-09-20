using GreenCrescent.Application.Common.Models;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public interface IOrphanApplicationService
{
    Task<SubmitOrphanApplicationResult> SubmitAsync(
        SubmitOrphanApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrphanApplicationListItemDto>>
        SearchAsync(
            string? searchTerm,
            OrphanApplicationStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<OrphanApplicationTrackingDto?>
        GetTrackingAsync(
            string trackingCode,
            CancellationToken cancellationToken = default);

    Task MarkUnderReviewAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<OrphanApplicationDetailsDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> ApproveAsync(
        ApproveOrphanApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        RejectOrphanApplicationRequest request,
        CancellationToken cancellationToken = default);
}