using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>The valuation summary block under the bill — labels wide, values in the rightmost money column, as on the PDF.</summary>
internal static class ValuationExportSummaryFooter
{
    public static void Add(ExcelSheet sheet, IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        var heading = Enumerable.Repeat<object?>(new ExcelStyledCell(null, SectionHeadFill), ValuationExportTitleBand.ColumnCount).ToArray();
        heading[0] = new ExcelStyledCell("Valuation summary", SectionHead);
        sheet.AddRow(heading);
        foreach (var row in summary)
        {
            AddSummaryRow(sheet, row);
        }
    }

    private static void AddSummaryRow(ExcelSheet sheet, ValuationExportSummaryRow row)
    {
        var fill = row.Strong ? SummaryFillStrong : Plain;
        var cells = Enumerable.Repeat<object?>(new ExcelStyledCell(null, fill), ValuationExportTitleBand.ColumnCount).ToArray();
        cells[1] = new ExcelStyledCell(row.Label, row.Strong ? SummaryLabelStrong : SummaryLabel);
        cells[^1] = new ExcelStyledCell(row.Amount, MoneyStyleFor(row));
        sheet.AddRow(cells);
    }

    private static ExcelCellStyle MoneyStyleFor(ValuationExportSummaryRow row)
    {
        if (row.Strong) return SummaryMoneyStrong;
        return row.Amount < 0m ? SummaryMoneyNegative : SummaryMoney;
    }
}
