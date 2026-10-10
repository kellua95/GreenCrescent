using GreenCrescent.Core.Common;

namespace GreenCrescent.Core.Entities;

public sealed class FinancialEntryRevision : BaseEntity
{
    public int FinancialEntryId { get; set; }

    public FinancialEntry FinancialEntry { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;

    public string PerformedByUserId { get; set; } = string.Empty;

    // نسخة من بيانات الحركة وأثرها المالي قبل التعديل.
    public string BeforeJson { get; set; } = string.Empty;

    // نسخة من بيانات الحركة وأثرها المالي بعد التعديل.
    public string AfterJson { get; set; } = string.Empty;
}