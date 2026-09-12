using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.FinancialEntries;

public sealed record CreateFinancialEntryRequest(
    int? SponsorId,
    string? DonorName,
    FinancialEntryType EntryType,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? BookNumber,
    string? ReceiptNumber,
    DateOnly EntryDate,
    string? Notes);