namespace GreenCrescent.Application.Features.OrphanApplications;

public sealed record ApproveOrphanApplicationRequest(
    int ApplicationId,
    string? Notes);

public sealed record RejectOrphanApplicationRequest(
    int ApplicationId,
    string Reason);