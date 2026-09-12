using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.FinancialEntries;

public sealed record FinancialEntryDto(
    int Id,
    int? SponsorId,
    string DonorName,
    FinancialEntryType EntryType,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? BookNumber,
    string? ReceiptNumber,
    DateOnly EntryDate,
    FinancialEntryStatus Status,
    string? Notes);