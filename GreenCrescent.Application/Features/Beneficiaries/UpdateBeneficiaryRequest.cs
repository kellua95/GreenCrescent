namespace GreenCrescent.Application.Features.Beneficiaries;

public sealed record UpdateBeneficiaryRequest(
    int Id,
    int FileNumber,
    string Name,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Notes)
{
    // null تعني أن المستدعي لم يرسل تعديلات لهذه البيانات.
    public BeneficiaryAdditionalData? AdditionalData { get; init; }

    // false تعني إبقاء الصورة الحالية كما هي.
    public bool ChangePhoto { get; init; }

    // عند ChangePhoto = true:
    // وجود البيانات يستبدل الصورة، وnull يزيلها.
    public byte[]? PhotoData { get; init; }

    public string? PhotoContentType { get; init; }
}