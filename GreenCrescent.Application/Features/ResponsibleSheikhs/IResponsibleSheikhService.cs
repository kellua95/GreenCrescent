namespace GreenCrescent.Application.Features.ResponsibleSheikhs;

public interface IResponsibleSheikhService
{
    Task<IReadOnlyList<ResponsibleSheikhDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResponsibleSheikhDto>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        string name,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
} 