using GreenCrescent.Application.Common;
using GreenCrescent.Application.Common.Models;
using GreenCrescent.Application.Features.OrphanApplications;
using GreenCrescent.Core.Entities;
using GreenCrescent.Core.Enums;
using GreenCrescent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenCrescent.Infrastructure.Services;

public sealed class OrphanApplicationService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IOrphanApplicationService
{
    public async Task<SubmitOrphanApplicationResult>
        SubmitAsync(
            SubmitOrphanApplicationRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateSubmission(request);
        var photoContentType = ValidatePhoto(request.PhotoData);

        var nationalNumber =
            request.OrphanNationalNumber.Trim();

        var beneficiaryExists =
            await dbContext.Beneficiaries.AnyAsync(
                item =>
                    item.NationalNumber == nationalNumber,
                cancellationToken);

        if (beneficiaryExists)
        {
            throw new InvalidOperationException(
                "يوجد مكفول مسجل بهذا الرقم الوطني.");
        }

        var applicationExists =
            await dbContext.OrphanApplications.AnyAsync(
                item =>
                    item.OrphanNationalNumber ==
                    nationalNumber,
                cancellationToken);

        if (applicationExists)
        {
            throw new InvalidOperationException(
                "يوجد طلب سابق مسجل بهذا الرقم الوطني. يرجى مراجعة الجمعية باستخدام رقم التتبع السابق.");
        }

        var submittedAtUtc = DateTime.UtcNow;

        var submissionDate =
            DateOnly.FromDateTime(submittedAtUtc);

        var automaticallyRejected =
            HasReachedMaximumAge(
                request.DateOfBirth,
                submissionDate);

        var application = new OrphanApplication
        {
            ApplicantName =
                request.ApplicantName.Trim(),

            ApplicantPhoneNumber =
                request.ApplicantPhoneNumber.Trim(),

            ApplicantRelationship =
                NormalizeOptional(
                    request.ApplicantRelationship),

            OrphanFirstName =
                request.OrphanFirstName.Trim(),

            OrphanFatherName =
                request.OrphanFatherName.Trim(),

            OrphanGrandfatherName =
                NormalizeOptional(
                    request.OrphanGrandfatherName),

            OrphanFamilyName =
                request.OrphanFamilyName.Trim(),

            OrphanNationalNumber =
                nationalNumber,

            DateOfBirth =
                request.DateOfBirth,

            Nationality =
                request.Nationality.Trim(),

            Gender =
                request.Gender,

            Address =
                request.Address.Trim(),

            PhoneNumber =
                NormalizeOptional(
                    request.PhoneNumber),

            PhotoData = request.PhotoData?.ToArray(),
            PhotoContentType = photoContentType,

            FatherDeathDate =
                request.FatherDeathDate,

            FatherDeathReason =
                request.FatherDeathReason.Trim(),

            MotherName =
                request.MotherName.Trim(),

            MotherNationalNumber =
                NormalizeOptional(
                    request.MotherNationalNumber),

            MotherDateOfBirth =
                request.MotherDateOfBirth,

            MotherNationality =
                NormalizeOptional(
                    request.MotherNationality),

            IsMotherAlive =
                request.IsMotherAlive,

            MotherPhoneNumber =
                NormalizeOptional(
                    request.MotherPhoneNumber),

            GuardianName =
                request.GuardianName.Trim(),

            GuardianNationalNumber =
                request.GuardianNationalNumber.Trim(),

            GuardianDateOfBirth =
                request.GuardianDateOfBirth,

            GuardianNationality =
                NormalizeOptional(
                    request.GuardianNationality),

            GuardianGender =
                request.GuardianGender,

            GuardianPhoneNumber =
                request.GuardianPhoneNumber.Trim(),

            GuardianRelationship =
                NormalizeOptional(
                    request.GuardianRelationship),

            FamilyMembersCount =
                request.FamilyMembersCount,

            SponsoredFamilyMembersCount =
                request.SponsoredFamilyMembersCount,

            HasIllness =
                request.HasIllness,

            IllnessDescription =
                request.HasIllness
                    ? NormalizeOptional(
                        request.IllnessDescription)
                    : null,

            HasHealthInsurance =
                request.HasHealthInsurance,

            HealthInsuranceProvider =
                request.HasHealthInsurance
                    ? NormalizeOptional(
                        request.HealthInsuranceProvider)
                    : null,

            EducationStage =
                NormalizeOptional(
                    request.EducationStage),

            AcademicAchievement =
                NormalizeOptional(
                    request.AcademicAchievement),

            SchoolDropoutReason =
                NormalizeOptional(
                    request.SchoolDropoutReason),

            AlternativeDirection =
                NormalizeOptional(
                    request.AlternativeDirection),

            TotalMonthlyIncome =
                request.TotalMonthlyIncome,

            TotalMonthlyExpenses =
                request.TotalMonthlyExpenses,

            Notes =
                NormalizeOptional(request.Notes),

            SubmittedAtUtc =
                submittedAtUtc,

            Status = automaticallyRejected
                ? OrphanApplicationStatus.Rejected
                : OrphanApplicationStatus.Submitted,

            IsAutomaticallyRejected =
                automaticallyRejected,

            ReviewedAtUtc = automaticallyRejected
                ? submittedAtUtc
                : null,

            DecisionReason = automaticallyRejected
                ? "تم رفض الطلب تلقائيًا لأن اليتيم بلغ الحد الأعلى المسموح وهو 15 سنة."
                : null,

            FamilyMembers = request.FamilyMembers
                .Select(item =>
                    new OrphanApplicationFamilyMember
                    {
                        Name = item.Name.Trim(),
                        DateOfBirth =
                            item.DateOfBirth,
                        Gender =
                            item.Gender,
                        Relationship =
                            NormalizeOptional(
                                item.Relationship),
                        EducationalStatus =
                            NormalizeOptional(
                                item.EducationalStatus),
                        SocialStatus =
                            NormalizeOptional(
                                item.SocialStatus),
                        HealthStatus =
                            NormalizeOptional(
                                item.HealthStatus),
                        Occupation =
                            NormalizeOptional(
                                item.Occupation),
                        MonthlyIncome =
                            item.MonthlyIncome
                    })
                .ToList()
        };

        dbContext.OrphanApplications.Add(
            application);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new SubmitOrphanApplicationResult(
            application.Id,
            application.TrackingCode,
            application.Status,
            application.IsAutomaticallyRejected,
            application.DecisionReason);
    }

    public async Task<
        PagedResult<OrphanApplicationListItemDto>>
        SearchAsync(
            string? searchTerm,
            OrphanApplicationStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        await EnsureApplicationsAccessAsync(cancellationToken);

        pageNumber = Math.Max(1, pageNumber);

        pageSize = pageSize is 10 or 25 or 50
            ? pageSize
            : 10;

        var query =
            dbContext.OrphanApplications
                .AsNoTracking()
                .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(item =>
                item.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(item =>
                EF.Functions.ILike(
                    item.OrphanFirstName,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    item.OrphanFatherName,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    item.OrphanFamilyName,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    item.OrphanNationalNumber,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    item.TrackingCode,
                    $"%{term}%") ||
                EF.Functions.ILike(
                    item.ApplicantPhoneNumber,
                    $"%{term}%"));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var rawItems = await query
            .OrderByDescending(item =>
                item.SubmittedAtUtc)
            .ThenByDescending(item =>
                item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Id,
                item.TrackingCode,
                item.OrphanFirstName,
                item.OrphanFatherName,
                item.OrphanGrandfatherName,
                item.OrphanFamilyName,
                item.OrphanNationalNumber,
                item.DateOfBirth,
                item.ApplicantName,
                item.ApplicantPhoneNumber,
                item.Status,
                item.IsAutomaticallyRejected,
                item.SubmittedAtUtc,
                item.ReviewedAtUtc,
                item.DecisionReason,
                item.BeneficiaryId
            })
            .ToListAsync(cancellationToken);

        var items = rawItems
            .Select(item =>
                new OrphanApplicationListItemDto(
                    item.Id,
                    BuildFullName(
                        item.OrphanFirstName,
                        item.OrphanFatherName,
                        item.OrphanGrandfatherName,
                        item.OrphanFamilyName),
                    item.TrackingCode,
                    item.OrphanNationalNumber,
                    item.DateOfBirth,
                    CalculateAge(
                        item.DateOfBirth,
                        DateOnly.FromDateTime(
                            item.SubmittedAtUtc)),
                    item.ApplicantName,
                    item.ApplicantPhoneNumber,
                    item.Status,
                    item.IsAutomaticallyRejected,
                    item.SubmittedAtUtc,
                    item.ReviewedAtUtc,
                    item.DecisionReason,
                    item.BeneficiaryId))
            .ToList();

        return new PagedResult<
            OrphanApplicationListItemDto>(
                items,
                totalCount,
                pageNumber,
                pageSize);
    }

    public async Task<OrphanApplicationTrackingDto?>
        GetTrackingAsync(
            string trackingCode,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var item =
            await dbContext.OrphanApplications
                .AsNoTracking()
                .Where(application =>
                    application.TrackingCode ==
                    trackingCode.Trim())
                .Select(application => new
                {
                    application.TrackingCode,
                    application.OrphanFirstName,
                    application.OrphanFatherName,
                    application.OrphanGrandfatherName,
                    application.OrphanFamilyName,
                    application.Status,
                    application.SubmittedAtUtc,
                    application.ReviewedAtUtc,
                    application.DecisionReason
                })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (item is null)
        {
            return null;
        }

        return new OrphanApplicationTrackingDto(
            item.TrackingCode,
            BuildFullName(
                item.OrphanFirstName,
                item.OrphanFatherName,
                item.OrphanGrandfatherName,
                item.OrphanFamilyName),
            item.Status,
            item.SubmittedAtUtc,
            item.ReviewedAtUtc,
            item.DecisionReason);
    }

    public async Task<OrphanApplicationDetailsDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        await EnsureApplicationsAccessAsync(cancellationToken);
        var application =
            await dbContext.OrphanApplications
                .AsNoTracking()
                .Include(item => item.FamilyMembers)
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

        if (application is null)
        {
            return null;
        }

        var familyMembers = application.FamilyMembers
            .OrderBy(item => item.Id)
            .Select(item =>
                new OrphanApplicationFamilyMemberDto(
                    item.Id,
                    item.Name,
                    item.DateOfBirth,
                    item.Gender,
                    item.Relationship,
                    item.EducationalStatus,
                    item.SocialStatus,
                    item.HealthStatus,
                    item.Occupation,
                    item.MonthlyIncome))
            .ToList();

        return new OrphanApplicationDetailsDto(
            application.Id,
            application.TrackingCode,

            application.ApplicantName,
            application.ApplicantPhoneNumber,
            application.ApplicantRelationship,

            application.OrphanFirstName,
            application.OrphanFatherName,
            application.OrphanGrandfatherName,
            application.OrphanFamilyName,
            application.OrphanNationalNumber,
            application.DateOfBirth,
            application.Nationality,
            application.Gender,
            application.Address,
            application.PhoneNumber,

            application.FatherDeathDate,
            application.FatherDeathReason,

            application.MotherName,
            application.MotherNationalNumber,
            application.MotherDateOfBirth,
            application.MotherNationality,
            application.IsMotherAlive,
            application.MotherPhoneNumber,

            application.GuardianName,
            application.GuardianNationalNumber,
            application.GuardianDateOfBirth,
            application.GuardianNationality,
            application.GuardianGender,
            application.GuardianPhoneNumber,
            application.GuardianRelationship,

            application.FamilyMembersCount,
            application.SponsoredFamilyMembersCount,

            application.HasIllness,
            application.IllnessDescription,
            application.HasHealthInsurance,
            application.HealthInsuranceProvider,

            application.EducationStage,
            application.AcademicAchievement,
            application.SchoolDropoutReason,
            application.AlternativeDirection,

            application.TotalMonthlyIncome,
            application.TotalMonthlyExpenses,

            application.Status,
            application.SubmittedAtUtc,
            application.ReviewedAtUtc,
            application.ReviewedByUserId,
            application.DecisionReason,
            application.IsAutomaticallyRejected,
            application.BeneficiaryId,
            application.Notes,

            familyMembers)
            {
                PhotoData = application.PhotoData,
                PhotoContentType = application.PhotoContentType
            };
    }

    public async Task MarkUnderReviewAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = await EnsureApplicationsAccessAsync(
            cancellationToken);
        var application =
            await dbContext.OrphanApplications
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == applicationId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "طلب اليتيم غير موجود.");

        if (application.Status ==
            OrphanApplicationStatus.Approved)
        {
            throw new InvalidOperationException(
                "تمت الموافقة على هذا الطلب مسبقًا.");
        }

        if (application.IsAutomaticallyRejected)
        {
            throw new InvalidOperationException(
                "لا يمكن إعادة فتح طلب مرفوض تلقائيًا بسبب العمر.");
        }

        application.Status =
            OrphanApplicationStatus.UnderReview;

        application.ReviewedByUserId =
            await currentUserService.GetUserIdAsync();

        application.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<int> ApproveAsync(
        ApproveOrphanApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = await EnsureApplicationsAccessAsync(
            cancellationToken);
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var application =
            await dbContext.OrphanApplications
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM "OrphanApplications"
                    WHERE "Id" = {request.ApplicationId}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "طلب اليتيم غير موجود.");

        if (application.Status ==
                OrphanApplicationStatus.Approved ||
            application.BeneficiaryId.HasValue)
        {
            throw new InvalidOperationException(
                "تمت الموافقة على هذا الطلب مسبقًا.");
        }

        if (application.IsAutomaticallyRejected)
        {
            throw new InvalidOperationException(
                "لا يمكن قبول الطلب لأن اليتيم بلغ 15 سنة عند تقديمه.");
        }

        var nationalNumberExists =
            await dbContext.Beneficiaries.AnyAsync(
                item =>
                    item.NationalNumber ==
                    application.OrphanNationalNumber,
                cancellationToken);

        if (nationalNumberExists)
        {
            throw new InvalidOperationException(
                "يوجد مكفول مسجل بهذا الرقم الوطني.");
        }

        // يمنع موظفين من توليد رقم الملف نفسه.
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            LOCK TABLE "Beneficiaries"
            IN SHARE ROW EXCLUSIVE MODE
            """,
            cancellationToken);

        var lastFileNumber =
            await dbContext.Beneficiaries
                .MaxAsync(
                    item => (int?)item.FileNumber,
                    cancellationToken)
            ?? 0;

        var beneficiary = new Beneficiary
        {
            FileNumber =
                checked(lastFileNumber + 1),

            Name = BuildFullName(
                application.OrphanFirstName,
                application.OrphanFatherName,
                application.OrphanGrandfatherName,
                application.OrphanFamilyName),

            NationalNumber =
                application.OrphanNationalNumber,

            Gender =
                application.Gender,

            Nationality =
                application.Nationality,

            Address =
                application.Address,

            PhoneNumber =
                application.PhoneNumber,

            DateOfBirth =
                application.DateOfBirth,

            PhotoData = application.PhotoData?.ToArray(),
            PhotoContentType = application.PhotoContentType,

            FamilyMembersCount = application.FamilyMembersCount,

            TotalMonthlyIncome = application.TotalMonthlyIncome,

            GuardianName =
                application.GuardianName,

            GuardianNationalNumber =
                application.GuardianNationalNumber,

            GuardianPhoneNumber =
                application.GuardianPhoneNumber,

            GuardianRelationship =
                application.GuardianRelationship,

            FatherDeathDate =
                application.FatherDeathDate,

            FatherDeathReason =
                application.FatherDeathReason,

            Status =
                BeneficiaryStatus.WaitingForSponsor,

            Notes =
                NormalizeOptional(request.Notes)
        };

        dbContext.Beneficiaries.Add(
            beneficiary);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        application.BeneficiaryId =
            beneficiary.Id;

        application.Status =
            OrphanApplicationStatus.Approved;

        application.IsAutomaticallyRejected =
            false;

        application.DecisionReason =
            NormalizeOptional(request.Notes);

        application.ReviewedAtUtc =
            DateTime.UtcNow;

        application.ReviewedByUserId =
            await currentUserService.GetUserIdAsync();

        application.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return beneficiary.Id;
    }

    public async Task RejectAsync(
        RejectOrphanApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = await EnsureApplicationsAccessAsync(
            cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException(
                "سبب الرفض مطلوب.");
        }

        if (request.Reason.Trim().Length > 1000)
        {
            throw new InvalidOperationException(
                "سبب الرفض يجب ألا يتجاوز 1000 حرف.");
        }

        var application =
            await dbContext.OrphanApplications
                .FirstOrDefaultAsync(
                    item =>
                        item.Id ==
                        request.ApplicationId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "طلب اليتيم غير موجود.");

        if (application.Status ==
            OrphanApplicationStatus.Approved)
        {
            throw new InvalidOperationException(
                "لا يمكن رفض طلب تمت الموافقة عليه.");
        }

        if (application.IsAutomaticallyRejected)
        {
            throw new InvalidOperationException(
                "الطلب مرفوض تلقائيًا بالفعل.");
        }

        application.Status =
            OrphanApplicationStatus.Rejected;

        application.DecisionReason =
            request.Reason.Trim();

        application.IsAutomaticallyRejected =
            false;

        application.ReviewedAtUtc =
            DateTime.UtcNow;

        application.ReviewedByUserId =
            await currentUserService.GetUserIdAsync();

        application.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<string> EnsureApplicationsAccessAsync(
    CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "يجب تسجيل الدخول للوصول إلى الطلبات.");
        }

        var hasAccess = await (
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles
                on user.Id equals userRole.UserId
            join role in dbContext.Roles
                on userRole.RoleId equals role.Id
            where user.Id == userId
                  && user.IsActive
                  && (
                      role.Name == AppRoles.Admin ||
                      role.Name == AppRoles.ApplicationsStaff
                  )
            select user.Id
        ).AnyAsync(cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "ليس لديك صلاحية الوصول إلى الطلبات أو مراجعتها.");
        }

        return userId;
    }

    private static bool HasReachedMaximumAge(
        DateOnly dateOfBirth,
        DateOnly submissionDate)
    {
        return dateOfBirth.AddYears(15) <=
               submissionDate;
    }

    private static int CalculateAge(
        DateOnly dateOfBirth,
        DateOnly referenceDate)
    {
        var age =
            referenceDate.Year -
            dateOfBirth.Year;

        if (dateOfBirth >
            referenceDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static void ValidateSubmission(
        SubmitOrphanApplicationRequest request)
    {
        Require(
            request.ApplicantName,
            "اسم مقدم الطلب مطلوب.");

        Require(
            request.ApplicantPhoneNumber,
            "هاتف مقدم الطلب مطلوب.");

        Require(
            request.OrphanFirstName,
            "اسم اليتيم الأول مطلوب.");

        Require(
            request.OrphanFatherName,
            "اسم والد اليتيم مطلوب.");

        Require(
            request.OrphanFamilyName,
            "اسم عائلة اليتيم مطلوب.");

        Require(
            request.OrphanNationalNumber,
            "الرقم الوطني لليتيم مطلوب.");

        Require(
            request.Nationality,
            "جنسية اليتيم مطلوبة.");

        Require(
            request.Address,
            "عنوان اليتيم مطلوب.");

        Require(
            request.FatherDeathReason,
            "سبب وفاة الأب مطلوب.");

        Require(
            request.MotherName,
            "اسم الأم مطلوب.");

        Require(
            request.GuardianName,
            "اسم المعيل الحالي مطلوب.");

        Require(
            request.GuardianNationalNumber,
            "الرقم الوطني للمعيل مطلوب.");

        Require(
            request.GuardianPhoneNumber,
            "رقم هاتف المعيل مطلوب.");

        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.DateOfBirth > today)
        {
            throw new InvalidOperationException(
                "تاريخ ميلاد اليتيم لا يمكن أن يكون في المستقبل.");
        }

        if (request.FatherDeathDate > today)
        {
            throw new InvalidOperationException(
                "تاريخ وفاة الأب لا يمكن أن يكون في المستقبل.");
        }

        if (request.FamilyMembersCount < 1)
        {
            throw new InvalidOperationException(
                "عدد أفراد الأسرة يجب أن يكون واحدًا على الأقل.");
        }

        if (request.SponsoredFamilyMembersCount < 0 ||
            request.SponsoredFamilyMembersCount >
            request.FamilyMembersCount)
        {
            throw new InvalidOperationException(
                "عدد المكفولين من الأسرة غير صحيح.");
        }

        if (request.TotalMonthlyIncome < 0 ||
            request.TotalMonthlyExpenses < 0)
        {
            throw new InvalidOperationException(
                "الدخل والمصروفات لا يمكن أن تكون سالبة.");
        }

        if (request.HasIllness &&
            string.IsNullOrWhiteSpace(
                request.IllnessDescription))
        {
            throw new InvalidOperationException(
                "يرجى توضيح مرض اليتيم.");
        }

        if (request.HasHealthInsurance &&
            string.IsNullOrWhiteSpace(
                request.HealthInsuranceProvider))
        {
            throw new InvalidOperationException(
                "يرجى تحديد جهة التأمين الصحي.");
        }

        if (request.FamilyMembers.Any(item =>
                string.IsNullOrWhiteSpace(item.Name)))
        {
            throw new InvalidOperationException(
                "اسم كل فرد مضاف إلى الأسرة مطلوب.");
        }

        if (request.FamilyMembers.Any(item =>
                item.MonthlyIncome < 0))
        {
            throw new InvalidOperationException(
                "دخل فرد الأسرة لا يمكن أن يكون سالبًا.");
        }
    }

    private static void Require(
        string? value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                message);
        }
    }

    private static string BuildFullName(
        string firstName,
        string fatherName,
        string? grandfatherName,
        string familyName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                fatherName,
                grandfatherName,
                familyName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value =>
                value!.Trim()));
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? ValidatePhoto(byte[]? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data.Length == 0 || data.Length > 500 * 1024)
        {
            throw new InvalidOperationException(
                "حجم الصورة يجب أن يكون بين 1 بايت و500 كيلوبايت.");
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