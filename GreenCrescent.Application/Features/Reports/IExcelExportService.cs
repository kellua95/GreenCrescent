using GreenCrescent.Application.Features.FinancialEntries;

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
}