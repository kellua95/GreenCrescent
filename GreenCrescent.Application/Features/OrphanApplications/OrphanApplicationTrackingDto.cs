using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record OrphanApplicationTrackingDto(
    string TrackingCode,
    string OrphanName,
    OrphanApplicationStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionReason);