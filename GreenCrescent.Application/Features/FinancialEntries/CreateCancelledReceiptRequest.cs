namespace GreenCrescent.Application.Features.FinancialEntries;

public sealed record CreateCancelledReceiptRequest(
    string BookNumber,
    string ReceiptNumber,
    DateOnly EntryDate,
    string Reason);