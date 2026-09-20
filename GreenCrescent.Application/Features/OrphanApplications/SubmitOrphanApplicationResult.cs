using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record SubmitOrphanApplicationResult(
    int ApplicationId,
    string TrackingCode,
    OrphanApplicationStatus Status,
    bool WasAutomaticallyRejected,
    string? DecisionReason);