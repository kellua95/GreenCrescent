namespace GreenCrescent.Application.Common;

public interface IOrphanApplicationEmailService
{
    Task SendDecisionEmailAsync(
        string? toEmail,
        string applicantName,
        string orphanFullName,
        string trackingCode,
        bool isApproved,
        string? reason = null,
        CancellationToken cancellationToken = default);
}