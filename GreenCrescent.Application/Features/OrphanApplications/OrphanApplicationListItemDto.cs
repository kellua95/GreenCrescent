using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record OrphanApplicationListItemDto(
    int Id,
    string OrphanName,
        string TrackingCode,

    string OrphanNationalNumber,
    DateOnly DateOfBirth,
    int AgeAtSubmission,
    string ApplicantName,
    string ApplicantPhoneNumber,
    OrphanApplicationStatus Status,
    bool IsAutomaticallyRejected,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionReason,
    int? BeneficiaryId);