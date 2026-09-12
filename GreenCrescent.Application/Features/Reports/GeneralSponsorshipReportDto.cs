namespace GreenCrescent.Application.Features.Reports;

public sealed record GeneralSponsorshipReportDto(
    int SponsorshipId,
    int FileNumber,
    string BeneficiaryName,
    string? ResponsibleSheikhName,
    string SponsorName,
    string? SponsorPhoneNumber,
    decimal MonthlyAmount,
    DateOnly StartDate,
    DateOnly EndDate);