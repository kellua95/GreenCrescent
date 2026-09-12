using ClosedXML.Excel;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Reports;
using GreenCrescent.Core.Enums;

namespace GreenCrescent.Infrastructure.Services;

public sealed class ExcelExportService : IExcelExportService
{
    public byte[] ExportGeneral(
        IReadOnlyList<GeneralSponsorshipReportDto> rows,
        string reportTitle)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("التقرير");
        sheet.RightToLeft = true;

        var headers = new[]
        {
            "رقم الملف",
            "اسم المكفول",
            "اسم الكافل",
            "هاتف الكافل",
            "قيمة الكفالة",
            "تاريخ البداية",
            "تاريخ النهاية"
        };

        PrepareSheet(sheet, reportTitle, headers);

        var rowNumber = 3;

        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value = item.FileNumber;
            sheet.Cell(rowNumber, 2).Value = item.BeneficiaryName;
            sheet.Cell(rowNumber, 3).Value = item.SponsorName;
            sheet.Cell(rowNumber, 4).Value =
                item.SponsorPhoneNumber ?? string.Empty;
            sheet.Cell(rowNumber, 5).Value = item.MonthlyAmount;
            sheet.Cell(rowNumber, 6).Value =
                item.StartDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowNumber, 7).Value =
                item.EndDate.ToDateTime(TimeOnly.MinValue);

            rowNumber++;
        }

        sheet.Column(5).Style.NumberFormat.Format = "#,##0.000";
        sheet.Columns(6, 7).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, rowNumber);

        return Save(workbook);
    }

    public byte[] ExportDetailed(
        IReadOnlyList<DetailedSponsorshipReportDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("التقرير المفصل");
        sheet.RightToLeft = true;

        var headers = new[]
        {
            "رقم الملف",
            "اسم الكافل",
            "هاتف الكافل",
            "عنوان الكافل",
            "قيمة الكفالة",
            "تاريخ البداية",
            "تاريخ النهاية",
            "المعرف",
            "حالة الكفالة",
            "المكفول الحالي",
            "هاتف المكفول",
            "المكفول السابق الأول",
            "المكفول السابق الثاني",
            "المكفول السابق الثالث",
            "ملاحظات"
        };

        PrepareSheet(sheet, "التقرير المفصل للكفالات", headers);

        var rowNumber = 3;

        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value = item.FileNumber;
            sheet.Cell(rowNumber, 2).Value = item.SponsorName;
            sheet.Cell(rowNumber, 3).Value =
                item.SponsorPhoneNumber ?? string.Empty;
            sheet.Cell(rowNumber, 4).Value =
                item.SponsorAddress ?? string.Empty;
            sheet.Cell(rowNumber, 5).Value = item.MonthlyAmount;
            sheet.Cell(rowNumber, 6).Value =
                item.StartDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowNumber, 7).Value =
                item.EndDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowNumber, 8).Value = item.ResponsibleSheikhName ?? string.Empty;
            sheet.Cell(rowNumber, 9).Value =
                GetSponsorshipStatus(item.Status);
            sheet.Cell(rowNumber, 10).Value = item.BeneficiaryName;
            sheet.Cell(rowNumber, 11).Value =
                item.BeneficiaryPhoneNumber ?? string.Empty;
            sheet.Cell(rowNumber, 12).Value =
                item.FirstPreviousBeneficiaryName ?? string.Empty;
            sheet.Cell(rowNumber, 13).Value =
                item.SecondPreviousBeneficiaryName ?? string.Empty;
            sheet.Cell(rowNumber, 14).Value =
                item.ThirdPreviousBeneficiaryName ?? string.Empty;
            sheet.Cell(rowNumber, 15).Value =
                item.Notes ?? string.Empty;

            rowNumber++;
        }

        sheet.Column(10).Style.NumberFormat.Format = "#,##0.000";
        sheet.Columns(11, 12).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, rowNumber);

        return Save(workbook);
    }

    public byte[] ExportFinancial(
        IReadOnlyList<FinancialEntryDto> rows,
        string reportTitle)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("التقرير المالي");
        sheet.RightToLeft = true;

        var headers = new[]
        {
            "رقم الحركة",
            "اسم المتبرع",
            "نوع الإدخال",
            "القيمة",
            "طريقة الدفع",
            "رقم الدفتر",
            "رقم الوصل",
            "التاريخ",
            "الحالة",
            "ملاحظات"
        };

        PrepareSheet(sheet, reportTitle, headers);

        var rowNumber = 3;

        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value = item.Id;
            sheet.Cell(rowNumber, 2).Value = item.DonorName;
            sheet.Cell(rowNumber, 3).Value =
                GetEntryTypeName(item.EntryType);
            sheet.Cell(rowNumber, 4).Value = item.Amount;
            sheet.Cell(rowNumber, 5).Value =
                GetPaymentMethodName(item.PaymentMethod);
            sheet.Cell(rowNumber, 6).Value =
                item.BookNumber ?? string.Empty;
            sheet.Cell(rowNumber, 7).Value =
                item.ReceiptNumber ?? string.Empty;
            sheet.Cell(rowNumber, 8).Value =
                item.EntryDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowNumber, 9).Value =
                item.Status == FinancialEntryStatus.Confirmed
                    ? "مؤكدة"
                    : "ملغاة";
            sheet.Cell(rowNumber, 10).Value =
                item.Notes ?? string.Empty;

            rowNumber++;
        }

        var totalRow = rowNumber + 1;

        sheet.Cell(totalRow, 3).Value = "الإجمالي";
        sheet.Cell(totalRow, 4).Value = rows
            .Where(item =>
                item.Status == FinancialEntryStatus.Confirmed)
            .Sum(item => item.Amount);

        sheet.Range(totalRow, 3, totalRow, 4)
            .Style.Font.Bold = true;

        sheet.Column(4).Style.NumberFormat.Format = "#,##0.000";
        sheet.Column(8).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, totalRow + 1);

        return Save(workbook);
    }

    private static void PrepareSheet(
        IXLWorksheet sheet,
        string title,
        IReadOnlyList<string> headers)
    {
        sheet.Cell(1, 1).Value = title;

        sheet.Range(1, 1, 1, headers.Count).Merge();

        sheet.Cell(1, 1).Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(
                XLColor.FromHtml("#08783E"))
            .Alignment.SetHorizontal(
                XLAlignmentHorizontalValues.Center);

        for (var index = 0; index < headers.Count; index++)
        {
            sheet.Cell(2, index + 1).Value = headers[index];
        }

        var headerRange =
            sheet.Range(2, 1, 2, headers.Count);

        headerRange.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(
                XLColor.FromHtml("#123C28"))
            .Alignment.SetHorizontal(
                XLAlignmentHorizontalValues.Center);
    }

    private static void FinishSheet(
        IXLWorksheet sheet,
        int columnCount,
        int lastRow)
    {
        if (lastRow >= 3)
        {
            var dataRange =
                sheet.Range(2, 1, lastRow, columnCount);

            dataRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            dataRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;
        }

        sheet.SheetView.FreezeRows(2);
        sheet.Columns().AdjustToContents();

        foreach (var column in sheet.ColumnsUsed())
        {
            if (column.Width > 45)
            {
                column.Width = 45;
            }
        }
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetSponsorshipStatus(
        SponsorshipStatus status) => status switch
        {
            SponsorshipStatus.Active => "فعالة",
            SponsorshipStatus.Ended => "منتهية",
            SponsorshipStatus.Replaced => "مستبدلة",
            SponsorshipStatus.Cancelled => "ملغاة",
            _ => status.ToString()
        };

    private static string GetEntryTypeName(
        FinancialEntryType type) => type switch
        {
            FinancialEntryType.NewOrphan => "يتيم جديد",
            FinancialEntryType.Zakat => "زكاة",
            FinancialEntryType.Charity => "صدقة",
            FinancialEntryType.Other => "أخرى",
            FinancialEntryType.OrphanSponsorship => "كفالة يتيم",
            FinancialEntryType.SponsorshipDeliveredByHand =>
                "كفالة سلمت باليد",
            _ => type.ToString()
        };

    private static string GetPaymentMethodName(
        PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => "نقدي",
            PaymentMethod.Clearing => "مقاصة",
            PaymentMethod.ByHand => "باليد",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            _ => method.ToString()
        };
}