using ClosedXML.Excel;
using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Reports;
using GreenCrescent.Application.Features.Sponsors;
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
            "المعرّف",
            "قيمة الكفالة",
            "تاريخ البداية",
            "تاريخ النهاية"
        };

        var rowNumber = PrepareSheet(sheet, reportTitle, headers);
        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value = item.FileNumber;
            sheet.Cell(rowNumber, 2).Value = item.BeneficiaryName;
            sheet.Cell(rowNumber, 3).Value = item.SponsorName;
            sheet.Cell(rowNumber, 4).Value =
                item.SponsorPhoneNumber ?? string.Empty;
            sheet.Cell(rowNumber, 5).Value =
                item.ResponsibleSheikhName;
            sheet.Cell(rowNumber, 6).Value = item.MonthlyAmount;
            sheet.Cell(rowNumber, 7).Value =
                item.StartDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowNumber, 8).Value =
                item.EndDate.ToDateTime(TimeOnly.MinValue);

            rowNumber++;
        }

        sheet.Column(6).Style.NumberFormat.Format = "#,##0.000";
        sheet.Columns(7, 8).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, rowNumber - 1);

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

        var rowNumber = PrepareSheet(
            sheet,
            "التقرير المفصل للكفالات",
            headers);

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

        sheet.Column(5).Style.NumberFormat.Format = "#,##0.000";
        sheet.Columns(6, 7).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, rowNumber - 1);

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

        var rowNumber = PrepareSheet(sheet, reportTitle, headers);

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

        FinishSheet(sheet, headers.Length, totalRow);

        return Save(workbook);
    }

    public byte[] ExportSponsors(
        IReadOnlyList<SponsorDto> rows,
        string reportTitle)
    {
        using var workbook = new XLWorkbook();

        var sheet =
            workbook.Worksheets.Add("سجل الكفلاء");

        sheet.RightToLeft = true;

        var headers = new[]
        {
        "اسم الكافل",
        "رقم الهاتف",
        "العنوان",
        "عدد الكفالات الفعالة",
        "الرصيد المتاح",
        "الحالة"
    };

        var rowNumber =
            PrepareSheet(
                sheet,
                reportTitle,
                headers);

        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value =
                item.Name;

            sheet.Cell(rowNumber, 2).Value =
                item.PhoneNumber ?? string.Empty;

            sheet.Cell(rowNumber, 3).Value =
                item.Address ?? string.Empty;

            sheet.Cell(rowNumber, 4).Value =
                item.ActiveSponsorshipsCount;

            sheet.Cell(rowNumber, 5).Value =
                item.CreditBalance;

            sheet.Cell(rowNumber, 6).Value =
                item.IsActive
                    ? "فعّال"
                    : "موقوف";

            rowNumber++;
        }

        sheet.Column(5)
            .Style.NumberFormat.Format =
                "#,##0.000";

        FinishSheet(
            sheet,
            headers.Length,
            rowNumber - 1);

        return Save(workbook);
    }

    public byte[] ExportBeneficiaries(
        IReadOnlyList<BeneficiaryDto> rows,
        string reportTitle,
        bool archivedOnly)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(
            archivedOnly ? "أرشيف المكفولين" : "سجل المكفولين");
        sheet.RightToLeft = true;

        var headers = archivedOnly
            ? new[]
            {
                "رقم الملف",
                "اسم المكفول",
                "رقم الهاتف",
                "تاريخ الميلاد",
                "سبب الأرشفة",
                "ملاحظات"
            }
            : new[]
            {
                "رقم الملف",
                "اسم المكفول",
                "رقم الهاتف",
                "تاريخ الميلاد",
                "عدد الكفالات الفعالة",
                "الكافل",
                "الحالة",
                "ملاحظات"
            };

        var rowNumber = PrepareSheet(sheet, reportTitle, headers);

        foreach (var item in rows)
        {
            sheet.Cell(rowNumber, 1).Value = item.FileNumber;
            sheet.Cell(rowNumber, 2).Value = item.Name;
            sheet.Cell(rowNumber, 3).Value =
                item.PhoneNumber ?? string.Empty;

            if (item.DateOfBirth.HasValue)
            {
                sheet.Cell(rowNumber, 4).Value =
                    item.DateOfBirth.Value.ToDateTime(
                        TimeOnly.MinValue);
            }

            if (archivedOnly)
            {
                sheet.Cell(rowNumber, 5).Value =
                    item.ArchiveReason ?? string.Empty;
                sheet.Cell(rowNumber, 6).Value =
                    item.Notes ?? string.Empty;
            }
            else
            {
                sheet.Cell(rowNumber, 5).Value =
                    item.ActiveSponsorshipsCount;
                sheet.Cell(rowNumber, 6).Value =
                    item.ActiveSponsorName ?? string.Empty;
                sheet.Cell(rowNumber, 7).Value =
                    GetBeneficiaryStatus(item.Status);
                sheet.Cell(rowNumber, 8).Value =
                    item.Notes ?? string.Empty;
            }

            rowNumber++;
        }

        sheet.Column(4).Style.DateFormat.Format = "yyyy/MM/dd";

        FinishSheet(sheet, headers.Length, rowNumber - 1);

        return Save(workbook);
    }

    private static int PrepareSheet(
        IXLWorksheet sheet,
        string title,
        IReadOnlyList<string> headers)
    {
        var columnCount = headers.Count;

        // تقسيم عرض التقرير إلى ثلاثة أجزاء:
        // اسم الجمعية يمينًا، الشعار في الوسط، معلومات التقرير يسارًا.
        var sideColumnCount = Math.Max(
            2,
            (int)Math.Ceiling(columnCount / 3d));

        var associationEndColumn =
            Math.Min(sideColumnCount, columnCount - 2);

        var informationStartColumn =
            Math.Max(
                associationEndColumn + 2,
                columnCount - sideColumnCount + 1);

        var logoStartColumn =
            associationEndColumn + 1;

        var logoEndColumn =
            informationStartColumn - 1;

        var logoColumn =
            (logoStartColumn + logoEndColumn) / 2;

        // ارتفاع الترويسة.
        for (var row = 1; row <= 4; row++)
        {
            sheet.Row(row).Height = 23;
        }

        // الجزء الأيمن: اسم الجمعية.
        var associationRange =
            sheet.Range(
                1,
                1,
                2,
                associationEndColumn);

        associationRange.Merge();

        associationRange.Value =
            "جمعية الهلال الأخضر الخيرية";

        associationRange.Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Font.SetFontColor(
                XLColor.FromHtml("#17251D"))
            .Alignment.SetHorizontal(
                XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(
                XLAlignmentVerticalValues.Center);

        // الجزء الأيمن: اسم التقرير.
        var reportTitleRange =
            sheet.Range(
                3,
                1,
                4,
                associationEndColumn);

        reportTitleRange.Merge();
        reportTitleRange.Value = title;

        reportTitleRange.Style
            .Font.SetFontSize(12)
            .Alignment.SetHorizontal(
                XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(
                XLAlignmentVerticalValues.Center);

        // منطقة الشعار في الوسط.
        var logoRange =
            sheet.Range(
                1,
                logoStartColumn,
                4,
                logoEndColumn);

        logoRange.Merge();

        AddLogo(
            sheet,
            logoColumn);

        // الجزء الأيسر: بيانات التقرير.
        var branchRange =
            sheet.Range(
                1,
                informationStartColumn,
                1,
                columnCount);

        branchRange.Merge();
        branchRange.Value = "الفرع: إربد";

        var numberRange =
            sheet.Range(
                2,
                informationStartColumn,
                2,
                columnCount);

        numberRange.Merge();

        // تبقى فارغة حتى يتم إدخالها يدويًا.
        numberRange.Value = "الرقم:";

        var dateRange =
            sheet.Range(
                3,
                informationStartColumn,
                4,
                columnCount);

        dateRange.Merge();

        dateRange.Value =
            $"التاريخ: {DateTime.Now:yyyy/MM/dd}";

        foreach (var range in new[]
                 {
                 branchRange,
                 numberRange,
                 dateRange
             })
        {
            range.Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(
                    XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(
                    XLAlignmentVerticalValues.Center);
        }

        // إطار الترويسة بالكامل.
        var completeHeaderRange =
            sheet.Range(
                1,
                1,
                4,
                columnCount);

        completeHeaderRange.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        // فواصل عمودية بين أقسام الترويسة.
        associationRange.Style.Border.RightBorder =
            XLBorderStyleValues.Thin;

        logoRange.Style.Border.RightBorder =
            XLBorderStyleValues.Thin;

        logoRange.Style.Border.LeftBorder =
            XLBorderStyleValues.Thin;

        // عناوين أعمدة الجدول في الصف الخامس.
        for (var index = 0; index < headers.Count; index++)
        {
            sheet.Cell(5, index + 1).Value =
                headers[index];
        }

        var columnHeaderRange =
            sheet.Range(
                5,
                1,
                5,
                columnCount);

        columnHeaderRange.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(
                XLColor.FromHtml("#123C28"))
            .Alignment.SetHorizontal(
                XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(
                XLAlignmentVerticalValues.Center);

        columnHeaderRange.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        columnHeaderRange.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        sheet.Row(5).Height = 22;

        // البيانات تبدأ من الصف السادس.
        return 6;
    }

    private static void AddLogo(
        IXLWorksheet sheet,
        int logoColumn)
    {
        var assembly =
            typeof(ExcelExportService).Assembly;

        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name =>
                name.EndsWith(
                    "GreenCrescentLogo.png",
                    StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException(
                "لم يتم العثور على شعار الجمعية ضمن موارد المشروع.");
        }

        using var logoStream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                "تعذر قراءة شعار الجمعية.");

        sheet.AddPicture(logoStream)
            .MoveTo(sheet.Cell(1, logoColumn))
            .WithSize(120, 120);
    }

    private static void FinishSheet(
        IXLWorksheet sheet,
        int columnCount,
        int lastRow)
    {
        if (lastRow >= 4)
        {
            var dataRange =
                sheet.Range(5, 1, lastRow, columnCount);

            dataRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            dataRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;
        }

        sheet.SheetView.FreezeRows(5);
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

    private static string GetBeneficiaryStatus(
        BeneficiaryStatus status) => status switch
        {
            BeneficiaryStatus.WaitingForSponsor =>
                "بانتظار كافل",
            BeneficiaryStatus.Sponsored => "مكفول",
            BeneficiaryStatus.NoLongerEligible =>
                "غير مستحق",
            BeneficiaryStatus.Suspended => "موقوف",
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
