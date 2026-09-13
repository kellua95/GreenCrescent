using GreenCrescent.Application.Common.Models;
using GreenCrescent.Application.Features.Sponsors;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services
{
    public sealed class SponsorService(ApplicationDbContext dbContext) : ISponsorService
    {
        public async Task<IReadOnlyList<SponsorDto>> SearchAsync(
        string? searchTerm,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
        {
            var query = dbContext.Sponsors
                .AsNoTracking()
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(sponsor => sponsor.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                query = query.Where(sponsor =>
                    EF.Functions.ILike(
                        sponsor.Name,
                        $"%{term}%") ||
                    (
                        sponsor.PhoneNumber != null &&
                        EF.Functions.ILike(
                            sponsor.PhoneNumber,
                            $"%{term}%")
                    ));
            }

            return await query
                .OrderBy(sponsor => sponsor.Name)
                .Take(100)
                .Select(sponsor => new SponsorDto(
                    sponsor.Id,
                    sponsor.Name,
                    sponsor.PhoneNumber,
                    sponsor.Address,
                    sponsor.IsActive,
                    sponsor.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)))
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<SponsorDto>>
    SearchPageAsync(
        string? searchTerm,
        bool includeInactive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(
                1,
                pageNumber);

            pageSize = pageSize is 10 or 25 or 50
                ? pageSize
                : 10;

            var query = dbContext.Sponsors
                .AsNoTracking()
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(item =>
                    item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                query = query.Where(item =>
                    EF.Functions.ILike(
                        item.Name,
                        $"%{term}%") ||

                    (
                        item.PhoneNumber != null &&
                        EF.Functions.ILike(
                            item.PhoneNumber,
                            $"%{term}%")
                    ) ||

                    (
                        item.Address != null &&
                        EF.Functions.ILike(
                            item.Address,
                            $"%{term}%")
                    ));
            }

            var totalCount = await query.CountAsync(
                cancellationToken);

            var items = await query
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new SponsorDto(
                    item.Id,
                    item.Name,
                    item.PhoneNumber,
                    item.Address,
                    item.IsActive,
                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)))
                .ToListAsync(cancellationToken);

            return new PagedResult<SponsorDto>(
                items,
                totalCount,
                pageNumber,
                pageSize);
        }
        public async Task<SponsorDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Sponsors
                .AsNoTracking()
                .Where(sponsor => sponsor.Id == id)
                .Select(sponsor => new SponsorDto(
                    sponsor.Id,
                    sponsor.Name,
                    sponsor.PhoneNumber,
                    sponsor.Address,
                    sponsor.IsActive,
                    sponsor.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CreateAsync(
            CreateSponsorRequest request,
            CancellationToken cancellationToken = default)
        {
            Validate(
                request.Name,
                request.PhoneNumber,
                request.Address);

            var sponsor = new Sponsor
            {
                Name = request.Name.Trim(),
                PhoneNumber = NormalizeOptional(
                    request.PhoneNumber),
                Address = NormalizeOptional(request.Address)
            };

            dbContext.Sponsors.Add(sponsor);

            await dbContext.SaveChangesAsync(cancellationToken);

            return sponsor.Id;
        }

        public async Task UpdateAsync(
            UpdateSponsorRequest request,
            CancellationToken cancellationToken = default)
        {
            Validate(
                request.Name,
                request.PhoneNumber,
                request.Address);

            var sponsor = await dbContext.Sponsors
                .FirstOrDefaultAsync(
                    item => item.Id == request.Id,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "الكافل المطلوب غير موجود.");

            sponsor.Name = request.Name.Trim();
            sponsor.PhoneNumber =
                NormalizeOptional(request.PhoneNumber);
            sponsor.Address =
                NormalizeOptional(request.Address);
            sponsor.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task SetActiveAsync(
            int id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var sponsor = await dbContext.Sponsors
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "الكافل المطلوب غير موجود.");

            if (!isActive)
            {
                var hasActiveSponsorships =
                    await dbContext.Sponsorships.AnyAsync(
                        sponsorship =>
                            sponsorship.SponsorId == id &&
                            sponsorship.Status ==
                            SponsorshipStatus.Active,
                        cancellationToken);

                if (hasActiveSponsorships)
                {
                    throw new InvalidOperationException(
                        "لا يمكن إيقاف كافل لديه كفالات فعالة. استبدل الكافل أو أوقف كفالاته أولًا.");
                }
            }

            sponsor.IsActive = isActive;
            sponsor.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static void Validate(
            string name,
            string? phoneNumber,
            string? address)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException(
                    "اسم الكافل مطلوب.");
            }

            if (name.Trim().Length > 200)
            {
                throw new InvalidOperationException(
                    "اسم الكافل يجب ألا يتجاوز 200 حرف.");
            }

            if (phoneNumber?.Trim().Length > 30)
            {
                throw new InvalidOperationException(
                    "رقم الهاتف يجب ألا يتجاوز 30 حرفًا.");
            }

            if (address?.Trim().Length > 500)
            {
                throw new InvalidOperationException(
                    "العنوان يجب ألا يتجاوز 500 حرف.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
