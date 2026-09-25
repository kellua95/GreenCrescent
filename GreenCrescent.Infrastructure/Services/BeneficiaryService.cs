using GreenCrescent.Application.Common.Models;
using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class BeneficiaryService(
    ApplicationDbContext dbContext) : IBeneficiaryService
{
    public async Task<IReadOnlyList<BeneficiaryDto>> SearchAsync(
        string? searchTerm,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Beneficiaries
            .AsNoTracking()
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(beneficiary =>
                !beneficiary.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var isFileNumber =
                int.TryParse(term, out var fileNumber);

            query = query.Where(beneficiary =>
                EF.Functions.ILike(
                    beneficiary.Name,
                    $"%{term}%") ||
                (
                    beneficiary.PhoneNumber != null &&
                    EF.Functions.ILike(
                        beneficiary.PhoneNumber,
                        $"%{term}%")
                ) ||
                (
                    isFileNumber &&
                    beneficiary.FileNumber == fileNumber
                ));
        }

        return await query
            .OrderBy(beneficiary => beneficiary.FileNumber)
            .Take(100)
            .Select(beneficiary => new BeneficiaryDto(
                beneficiary.Id,
                beneficiary.FileNumber,
                beneficiary.Name,
                beneficiary.PhoneNumber,
                beneficiary.DateOfBirth,
                beneficiary.Status,
                beneficiary.IsArchived,
                beneficiary.ArchiveReason,
                beneficiary.Notes,
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        (int?)sponsorship.Id)
                    .FirstOrDefault(),
                beneficiary.Sponsorships
                    .Where(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active)
                    .Select(sponsorship =>
                        sponsorship.Sponsor.Name)
                    .FirstOrDefault(),

                beneficiary.Sponsorships.Count(sponsorship =>
                    sponsorship.Status ==
                    SponsorshipStatus.Active))
            {
                NationalNumber = beneficiary.NationalNumber,
                GuardianName = beneficiary.GuardianName,
                GuardianNationalNumber = beneficiary.GuardianNationalNumber,
                GuardianPhoneNumber = beneficiary.GuardianPhoneNumber,
                FamilyMembersCount = beneficiary.FamilyMembersCount,
                TotalMonthlyIncome = beneficiary.TotalMonthlyIncome
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<BeneficiaryDto>>
        SearchPageAsync(
            string? searchTerm,
            bool includeArchived,
            bool sponsorshipCountDescending,
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

            var query = dbContext.Beneficiaries
                .AsNoTracking()
                .AsQueryable();

            if (!includeArchived)
            {
                query = query.Where(item =>
                    !item.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                var isFileNumber =
                    int.TryParse(
                        term,
                        out var fileNumber);

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
                        isFileNumber &&
                        item.FileNumber == fileNumber
                    ));
            }

            var totalCount = await query.CountAsync(
                cancellationToken);

        var orderedQuery = sponsorshipCountDescending
            ? query
                .OrderByDescending(item =>
                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active))
                .ThenBy(item => item.FileNumber)
                .ThenBy(item => item.Id)

            : query
                .OrderBy(item =>
                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active))
                .ThenBy(item => item.FileNumber)
                .ThenBy(item => item.Id);

        var items = await orderedQuery
                        .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new BeneficiaryDto(
                    item.Id,
                    item.FileNumber,
                    item.Name,
                    item.PhoneNumber,
                    item.DateOfBirth,
                    item.Status,
                    item.IsArchived,
                    item.ArchiveReason,
                    item.Notes,

                    item.Sponsorships
                        .Where(sponsorship =>
                            sponsorship.Status ==
                            SponsorshipStatus.Active)
                        .OrderBy(sponsorship =>
                            sponsorship.Id)
                        .Select(sponsorship =>
                            (int?)sponsorship.Id)
                        .FirstOrDefault(),

                    item.Sponsorships
                        .Where(sponsorship =>
                            sponsorship.Status ==
                            SponsorshipStatus.Active)
                        .OrderBy(sponsorship =>
                            sponsorship.Id)
                        .Select(sponsorship =>
                            sponsorship.Sponsor.Name)
                        .FirstOrDefault(),

                    item.Sponsorships.Count(sponsorship =>
                        sponsorship.Status ==
                        SponsorshipStatus.Active))
                {
    NationalNumber = item.NationalNumber,
    GuardianName = item.GuardianName,
    GuardianNationalNumber = item.GuardianNationalNumber,
    GuardianPhoneNumber = item.GuardianPhoneNumber,
    FamilyMembersCount = item.FamilyMembersCount,
    TotalMonthlyIncome = item.TotalMonthlyIncome
})
                .ToListAsync(cancellationToken);

            return new PagedResult<BeneficiaryDto>(
                items,
                totalCount,
                pageNumber,
                pageSize);
        }

    public async Task<BeneficiaryDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Beneficiaries
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new BeneficiaryDto(
                item.Id,
                item.FileNumber,
                item.Name,
                item.PhoneNumber,
                item.DateOfBirth,
                item.Status,
                item.IsArchived,
                item.ArchiveReason,
                item.Notes,

                item.Sponsorships
                    .Where(s => s.Status == SponsorshipStatus.Active)
                    .OrderBy(s => s.Id)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefault(),

                item.Sponsorships
                    .Where(s => s.Status == SponsorshipStatus.Active)
                    .OrderBy(s => s.Id)
                    .Select(s => s.Sponsor.Name)
                    .FirstOrDefault(),

                item.Sponsorships.Count(
                    s => s.Status == SponsorshipStatus.Active))
            {
                NationalNumber = item.NationalNumber,
                Gender = item.Gender,
                Nationality = item.Nationality,
                Address = item.Address,

                GuardianName = item.GuardianName,
                GuardianNationalNumber = item.GuardianNationalNumber,
                GuardianPhoneNumber = item.GuardianPhoneNumber,
                GuardianRelationship = item.GuardianRelationship,

                FatherDeathDate = item.FatherDeathDate,
                FatherDeathReason = item.FatherDeathReason,

                FamilyMembersCount = item.FamilyMembersCount,
                TotalMonthlyIncome = item.TotalMonthlyIncome,

                PhotoData = item.PhotoData,
                PhotoContentType = item.PhotoContentType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        var guardianNumber = result.GuardianNationalNumber?.Trim();

        if (string.IsNullOrWhiteSpace(guardianNumber))
        {
            return result;
        }

        var familyCount = await dbContext.Sponsorships
            .AsNoTracking()
            .CountAsync(
                item =>
                    item.Status == SponsorshipStatus.Active &&
                    item.Beneficiary.GuardianNationalNumber != null &&
                    item.Beneficiary.GuardianNationalNumber!.Trim() ==
                        guardianNumber,
                cancellationToken);

        return result with
        {
            FamilyActiveSponsorshipsCount = familyCount
        };
    }

    public async Task<int> CreateAsync(
    CreateBeneficiaryRequest request,
    CancellationToken cancellationToken = default)
    {
        Validate(
            1,
            request.Name,
            request.PhoneNumber,
            request.Notes);

        ValidateBirthDate(request.DateOfBirth);
        ValidateAdditionalData(request.AdditionalData);

        var photoContentType = DetectPhotoContentType(request.PhotoData);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

                // يمنع عمليتي إضافة متزامنتين من اختيار الرقم نفسه.
                // يجب أن يستخدم مسار الموافقة على طلب اليتيم القفل نفسه.
                await dbContext.Database.ExecuteSqlRawAsync(
                    """
            LOCK TABLE "Beneficiaries" IN SHARE ROW EXCLUSIVE MODE;
            """,
                    cancellationToken);

        await EnsureNationalNumberAvailableAsync(
                    request.AdditionalData?.NationalNumber,
                    null,
                    cancellationToken);

        var lastFileNumber = await dbContext.Beneficiaries
            .MaxAsync(
                item => (int?)item.FileNumber,
                cancellationToken) ?? 0;

        var nextFileNumber = checked(lastFileNumber + 1);

        var beneficiary = new Beneficiary
        {
            FileNumber = nextFileNumber,
            Name = request.Name.Trim(),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            DateOfBirth = request.DateOfBirth,
            Notes = NormalizeOptional(request.Notes),
            Status = BeneficiaryStatus.WaitingForSponsor,
            PhotoData = request.PhotoData?.ToArray(),
            PhotoContentType = photoContentType
        };

        if (request.AdditionalData is { } additionalData)
        {
            ApplyAdditionalData(beneficiary, additionalData);
        }

        dbContext.Beneficiaries.Add(beneficiary);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return beneficiary.Id;
    }

    public async Task UpdateAsync(
    UpdateBeneficiaryRequest request,
    CancellationToken cancellationToken = default)
    {
        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == request.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        // نعتمد رقم الملف المحفوظ، ونتجاهل الرقم المرسل.
        Validate(
            beneficiary.FileNumber,
            request.Name,
            request.PhoneNumber,
            request.Notes);

        ValidateBirthDate(request.DateOfBirth);
        ValidateAdditionalData(request.AdditionalData);

        var photoContentType = request.ChangePhoto
            ? DetectPhotoContentType(request.PhotoData)
            : null;

        if (request.AdditionalData is { } additionalData)
        {
            await EnsureNationalNumberAvailableAsync(
                additionalData.NationalNumber,
                request.Id,
                cancellationToken);
        }

        beneficiary.Name = request.Name.Trim();
        beneficiary.PhoneNumber =
            NormalizeOptional(request.PhoneNumber);

        beneficiary.DateOfBirth = request.DateOfBirth;
        beneficiary.Notes = NormalizeOptional(request.Notes);

        if (request.AdditionalData is { } data)
        {
            ApplyAdditionalData(beneficiary, data);
        }

        if (request.ChangePhoto)
        {
            beneficiary.PhotoData = request.PhotoData?.ToArray();
            beneficiary.PhotoContentType = photoContentType;
        }

        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeStatusAsync(
        int id,
        BeneficiaryStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        if (newStatus == BeneficiaryStatus.Sponsored)
        {
            throw new InvalidOperationException(
                "تتحول حالة المكفول إلى مكفول تلقائيًا عند إنشاء كفالة.");
        }

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        var hasActiveSponsorship =
            await dbContext.Sponsorships.AnyAsync(
                sponsorship =>
                    sponsorship.BeneficiaryId == id &&
                    sponsorship.Status ==
                    SponsorshipStatus.Active,
                cancellationToken);

        if (hasActiveSponsorship)
        {
            throw new InvalidOperationException(
                "لا يمكن تغيير حالة مكفول لديه كفالة فعالة. أوقف الكفالة أو استبدل المكفول أولًا.");
        }

        beneficiary.Status = newStatus;
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
    int id,
    string reason,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "سبب الأرشفة مطلوب.");
        }

        if (reason.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الأرشفة يجب ألا يتجاوز 1000 حرف.");
        }

        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        var hasActiveSponsorship =
            await dbContext.Sponsorships.AnyAsync(
                item =>
                    item.BeneficiaryId == id &&
                    item.Status == SponsorshipStatus.Active,
                cancellationToken);

        if (hasActiveSponsorship)
        {
            throw new InvalidOperationException(
                "لا يمكن أرشفة مكفول لديه كفالة فعالة.");
        }

        beneficiary.IsArchived = true;
        beneficiary.ArchivedAtUtc = DateTime.UtcNow;
        beneficiary.ArchiveReason = reason.Trim();
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "المكفول المطلوب غير موجود.");

        beneficiary.IsArchived = false;
        beneficiary.ArchivedAtUtc = null;
        beneficiary.ArchiveReason = null;
        beneficiary.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(
        int fileNumber,
        string name,
        string? phoneNumber,
        string? notes)
    {
        if (fileNumber <= 0)
        {
            throw new InvalidOperationException(
                "رقم الملف يجب أن يكون أكبر من صفر.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "اسم المكفول مطلوب.");
        }

        if (name.Trim().Length > 200)
        {
            throw new InvalidOperationException(
                "اسم المكفول يجب ألا يتجاوز 200 حرف.");
        }

        if (phoneNumber?.Trim().Length > 30)
        {
            throw new InvalidOperationException(
                "رقم الهاتف يجب ألا يتجاوز 30 حرفًا.");
        }

        if (notes?.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ApplyAdditionalData(
    Beneficiary beneficiary,
    BeneficiaryAdditionalData data)
    {
        beneficiary.NationalNumber =
            NormalizeOptional(data.NationalNumber);

        beneficiary.Gender = data.Gender;
        beneficiary.Nationality = NormalizeOptional(data.Nationality);
        beneficiary.Address = NormalizeOptional(data.Address);

        beneficiary.GuardianName =
            NormalizeOptional(data.GuardianName);

        beneficiary.GuardianNationalNumber =
            NormalizeOptional(data.GuardianNationalNumber);

        beneficiary.GuardianPhoneNumber =
            NormalizeOptional(data.GuardianPhoneNumber);

        beneficiary.GuardianRelationship =
            NormalizeOptional(data.GuardianRelationship);

        beneficiary.FatherDeathDate = data.FatherDeathDate;

        beneficiary.FatherDeathReason =
            NormalizeOptional(data.FatherDeathReason);

        beneficiary.FamilyMembersCount = data.FamilyMembersCount;
        beneficiary.TotalMonthlyIncome = data.TotalMonthlyIncome;
    }

    private async Task EnsureNationalNumberAvailableAsync(
        string? nationalNumber,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeOptional(nationalNumber);

        if (normalized is null)
        {
            return;
        }

        var query = dbContext.Beneficiaries
            .Where(item =>
                item.NationalNumber != null &&
                item.NationalNumber.Trim() == normalized);

        if (excludedId.HasValue)
        {
            query = query.Where(item => item.Id != excludedId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "الرقم الوطني مسجل لمكفول آخر.");
        }
    }

    private static void ValidateBirthDate(DateOnly? dateOfBirth)
    {
        if (dateOfBirth.HasValue &&
            (dateOfBirth.Value == DateOnly.MinValue ||
             dateOfBirth.Value > TodayInJordan()))
        {
            throw new InvalidOperationException(
                "تاريخ الميلاد غير صحيح أو يقع في المستقبل.");
        }
    }

    private static DateOnly TodayInJordan()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");

        var now = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            zone);

        return DateOnly.FromDateTime(now.DateTime);
    }

    private static void ValidateAdditionalData(
        BeneficiaryAdditionalData? data)
    {
        if (data is null)
        {
            return;
        }

        ValidateOptionalLength(data.NationalNumber, 30, "الرقم الوطني");
        ValidateOptionalLength(data.Nationality, 100, "الجنسية");
        ValidateOptionalLength(data.Address, 500, "العنوان");

        ValidateOptionalLength(data.GuardianName, 200, "اسم المعيل");
        ValidateOptionalLength(
            data.GuardianNationalNumber, 30, "الرقم الوطني للمعيل");
        ValidateOptionalLength(
            data.GuardianPhoneNumber, 30, "هاتف المعيل");
        ValidateOptionalLength(
            data.GuardianRelationship, 100, "صلة القرابة");

        ValidateOptionalLength(
            data.FatherDeathReason, 500, "سبب وفاة الأب");

        if (data.Gender.HasValue &&
            !Enum.IsDefined(typeof(PersonGender), data.Gender.Value))
        {
            throw new InvalidOperationException("قيمة الجنس غير صحيحة.");
        }

        if (data.FamilyMembersCount.HasValue &&
            data.FamilyMembersCount.Value < 1)
        {
            throw new InvalidOperationException(
                "عدد أفراد الأسرة يجب أن يكون واحدًا على الأقل.");
        }

        if (data.TotalMonthlyIncome is decimal income)
        {
            if (income < 0 ||
                income > 999999999999999.999m)
            {
                throw new InvalidOperationException(
                    "قيمة دخل الأسرة خارج النطاق المسموح.");
            }

            if (decimal.Round(income, 3) != income)
            {
                throw new InvalidOperationException(
                    "دخل الأسرة يقبل ثلاث منازل عشرية كحد أقصى.");
            }
        }

        if (data.FatherDeathDate.HasValue &&
            (data.FatherDeathDate.Value == DateOnly.MinValue ||
             data.FatherDeathDate.Value > TodayInJordan()))
        {
            throw new InvalidOperationException(
                "تاريخ وفاة الأب غير صحيح أو يقع في المستقبل.");
        }

        // لا نقارن وفاة الأب بميلاد الطفل؛
        // قد يتوفى الأب قبل ولادته.
    }

    private static void ValidateOptionalLength(
        string? value,
        int maximum,
        string fieldName)
    {
        if (value?.Trim().Length > maximum)
        {
            throw new InvalidOperationException(
                $"{fieldName} يجب ألا يتجاوز {maximum} حرفًا.");
        }
    }

    private static string? DetectPhotoContentType(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data.Length == 0 || data.Length > 500 * 1024)
        {
            throw new InvalidOperationException(
                "يجب أن تكون الصورة غير فارغة وبحجم لا يتجاوز 500 كيلوبايت.");
        }

        var isPng =
            data.Length >= 8 &&
            data[0] == 0x89 &&
            data[1] == 0x50 &&
            data[2] == 0x4E &&
            data[3] == 0x47 &&
            data[4] == 0x0D &&
            data[5] == 0x0A &&
            data[6] == 0x1A &&
            data[7] == 0x0A;

        var isJpeg =
            data.Length >= 4 &&
            data[0] == 0xFF &&
            data[1] == 0xD8 &&
            data[2] == 0xFF &&
            data[^2] == 0xFF &&
            data[^1] == 0xD9;

        if (isPng)
        {
            return "image/png";
        }

        if (isJpeg)
        {
            return "image/jpeg";
        }

        throw new InvalidOperationException(
            "الملف لا يحمل توقيع صورة JPG أو PNG صالحًا.");
    }
}