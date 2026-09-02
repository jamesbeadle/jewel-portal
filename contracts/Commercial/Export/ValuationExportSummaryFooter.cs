using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Models;
using static Jewel.JPMS.Contracts.Commercial.Export.ValuationExportStyles;

namespace Jewel.JPMS.Contracts.Commercial.Export;

/// <summary>The valuation summary block under the bill — labels wide, values in the rightmost money column, as on the PDF.</summary>
internal static class ValuationExportSummaryFooter
{
    public static void Add(ExcelSheet sheet, IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        ValuationExportStatementSheet.AddHeadingRow(sheet,
            new ExcelStyledCell("Valuation summary", SectionHead), SectionHeadFill);
        foreach (var row in summary)
        {
            AddSummaryRow(sheet, row);
        }
    }

    private static void AddSummaryRow(ExcelSheet sheet, ValuationExportSummaryRow row)
    {
        var fill = row.Strong ? SummaryFillStrong : Plain;
        var cells = ValuationExportStatementSheet.FilledCells(sheet, fill);
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
