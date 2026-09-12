using GreenCrescent.Application.Features.ResponsibleSheikhs;
using GreenCrescent.Core.Entities;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Infrastructure.Services;

public sealed class ResponsibleSheikhService(
    ApplicationDbContext dbContext)
    : IResponsibleSheikhService
{
    public async Task<IReadOnlyList<ResponsibleSheikhDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResponsibleSheikhs
            .AsNoTracking()
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Name)
            .Select(item => new ResponsibleSheikhDto(
                item.Id,
                item.Name,
                item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResponsibleSheikhDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResponsibleSheikhs
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new ResponsibleSheikhDto(
                item.Id,
                item.Name,
                item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);

        var nameExists = await dbContext.ResponsibleSheikhs
            .AnyAsync(
                item => EF.Functions.ILike(
                    item.Name,
                    normalizedName),
                cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                "يوجد معرّف مسجل بهذا الاسم بالفعل.");
        }

        var responsibleSheikh = new ResponsibleSheikh
        {
            Name = normalizedName,
            IsActive = true
        };

        dbContext.ResponsibleSheikhs.Add(responsibleSheikh);

        await dbContext.SaveChangesAsync(cancellationToken);

        return responsibleSheikh.Id;
    }

    public async Task UpdateAsync(
        int id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);

        var responsibleSheikh =
            await dbContext.ResponsibleSheikhs
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "المعرّف المطلوب غير موجود.");

        var nameExists = await dbContext.ResponsibleSheikhs
            .AnyAsync(
                item =>
                    item.Id != id &&
                    EF.Functions.ILike(
                        item.Name,
                        normalizedName),
                cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                "يوجد معرّف آخر مسجل بهذا الاسم.");
        }

        responsibleSheikh.Name = normalizedName;
        responsibleSheikh.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var responsibleSheikh =
            await dbContext.ResponsibleSheikhs
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "المعرّف المطلوب غير موجود.");

        if (!isActive)
        {
            var hasActiveSponsorships =
                await dbContext.Sponsorships.AnyAsync(
                    sponsorship =>
                        sponsorship.ResponsibleSheikhId == id &&
                        sponsorship.Status ==
                        SponsorshipStatus.Active,
                    cancellationToken);

            if (hasActiveSponsorships)
            {
                throw new InvalidOperationException(
                    "لا يمكن إيقاف هذا المعرّف لأنه مسؤول عن كفالات فعالة.");
            }
        }

        responsibleSheikh.IsActive = isActive;
        responsibleSheikh.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "اسم المعرّف مطلوب.");
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 150)
        {
            throw new InvalidOperationException(
                "اسم المعرّف يجب ألا يتجاوز 150 حرفًا.");
        }

        return normalizedName;
    }
}