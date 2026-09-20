using GreenCrescent.Application.Common.Helpers;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Reports;

public sealed record DetailedSponsorshipReportDto(
    int SponsorshipId,
    int FileNumber,
    string BeneficiaryName,
    string? ResponsibleSheikhName,
    string? FirstPreviousBeneficiaryName,
    string? SecondPreviousBeneficiaryName,
    string? ThirdPreviousBeneficiaryName,
    string? BeneficiaryPhoneNumber,
    string SponsorName,
    string? SponsorPhoneNumber,
    string? SponsorAddress,
    decimal MonthlyAmount,
    DateOnly StartDate,
    DateOnly EndDate,
    SponsorshipStatus Status,
    string? Notes)
{
    public DateOnly? BeneficiaryDateOfBirth { get; init; }

    public int? BeneficiaryAge =>
        AgeCalculator.Calculate(
            BeneficiaryDateOfBirth,
            AgeCalculator.TodayInJordan());
}