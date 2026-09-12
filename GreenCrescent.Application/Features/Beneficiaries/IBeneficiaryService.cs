using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Beneficiaries;

public interface IBeneficiaryService
{
    Task<IReadOnlyList<BeneficiaryDto>> SearchAsync(
        string? searchTerm,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<BeneficiaryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        CreateBeneficiaryRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateBeneficiaryRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        int id,
        BeneficiaryStatus newStatus,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
    int id,
    string reason,
    CancellationToken cancellationToken = default);

    Task RestoreAsync(
        int id,
        CancellationToken cancellationToken = default);

}