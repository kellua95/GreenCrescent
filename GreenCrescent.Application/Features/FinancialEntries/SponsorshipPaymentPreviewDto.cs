namespace GreenCrescent.Application.Features.FinancialEntries;

public sealed record SponsorshipAllocationPreviewDto(
    int SponsorshipId,
    int FileNumber,
    string BeneficiaryName,
    decimal MonthlyAmount,
    DateOnly CurrentEndDate,
    DateOnly NewEndDate,
    decimal AllocatedAmount);

public sealed record SponsorshipPaymentPreviewDto(
    int SponsorId,
    string SponsorName,
    decimal TotalMonthlyAmount,
    decimal PaidAmount,
    int AddedMonths,
    IReadOnlyList<SponsorshipAllocationPreviewDto> Allocations,
    decimal PreviousCreditBalance = 0m,
    decimal AvailableCredit = 0m,
    decimal AppliedCredit = 0m,
    decimal RemainingCredit = 0m);