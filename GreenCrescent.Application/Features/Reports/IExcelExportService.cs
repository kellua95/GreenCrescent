using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Sponsors;

namespace GreenCrescent.Application.Features.Reports;

public interface IExcelExportService
{
    byte[] ExportGeneral(
        IReadOnlyList<GeneralSponsorshipReportDto> rows,
        string reportTitle);

    byte[] ExportDetailed(
        IReadOnlyList<DetailedSponsorshipReportDto> rows);

    byte[] ExportFinancial(
        IReadOnlyList<FinancialEntryDto> rows,
        string reportTitle);

    byte[] ExportSponsors(
        IReadOnlyList<SponsorDto> rows,
        string reportTitle);

    byte[] ExportBeneficiaries(
        IReadOnlyList<BeneficiaryDto> rows,
        string reportTitle,
        bool archivedOnly);
}
