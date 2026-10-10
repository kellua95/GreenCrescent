using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.FinancialEntries;

public sealed record UpdateFinancialEntryRequest(
    int Id,
    int ExpectedRevisionNumber,
    int? SponsorId,
    string? DonorName,
    FinancialEntryType EntryType,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string BookNumber,
    string ReceiptNumber,
    DateOnly EntryDate,
    string? Notes,
    bool IsCancelledReceipt,
    string ChangeReason);