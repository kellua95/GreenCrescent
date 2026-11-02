using GreenCrescent.Core.Enums;

namespace GreenCrescent.Application.Features.Sponsorships;

public sealed class SponsorshipDto
{
    public SponsorshipDto(
        int id,
        int sponsorId,
        string sponsorName,
        int beneficiaryId,
        int fileNumber,
        string beneficiaryName,
        int? responsibleSheikhId,
        string? responsibleSheikhName,
        decimal monthlyAmount,
        DateOnly startDate,
        DateOnly endDate,
        SponsorshipStatus status,
        string? notes)
    {
        Id = id;
        SponsorId = sponsorId;
        SponsorName = sponsorName;
        BeneficiaryId = beneficiaryId;
        FileNumber = fileNumber;
        BeneficiaryName = beneficiaryName;
        ResponsibleSheikhId = responsibleSheikhId;
        ResponsibleSheikhName = responsibleSheikhName;
        MonthlyAmount = monthlyAmount;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        Notes = notes;
    }

    public int Id { get; }
    public int SponsorId { get; }
    public string SponsorName { get; }
    public int BeneficiaryId { get; }
    public int FileNumber { get; }
    public string BeneficiaryName { get; }
    public int? ResponsibleSheikhId { get; }
    public string? ResponsibleSheikhName { get; }
    public decimal MonthlyAmount { get; }
    public DateOnly StartDate { get; }

    // تم تعديلها لتصبح قابلة للتعديل (set)
    public DateOnly EndDate { get; set; }

    public SponsorshipStatus Status { get; }
    public string? Notes { get; }
}