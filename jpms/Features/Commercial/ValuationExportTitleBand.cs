using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>The navy title band at the top of a statement tab, with the working-copy stamp and the
/// legend line under it. Sized from the sheet's own column grid, so the 10-column statement tabs
/// and the narrower Pending tab all carry the same band.</summary>
internal static class ValuationExportTitleBand
{
    private const string WorkingCopyWarning =
        "WORKING COPY — the live report as it stands right now; figures may change until the claim is locked.";

    public static void Add(ExcelSheet sheet, ValuationExportMeta meta, string legend)
    {
        AddSplitBandRow(sheet, new ExcelStyledCell("VALUATION REPORT", BandTitle),
            new ExcelStyledCell(meta.StatementLabel.ToUpperInvariant(), BandGoldRight));
        AddSplitBandRow(sheet, new ExcelStyledCell("Jewel Bespoke Build", BandGold),
            new ExcelStyledCell(meta.PreparedLabel, BandTextRight));
        if (meta.IsDraft) AddMergedRow(sheet, new ExcelStyledCell(WorkingCopyWarning, DraftWarning));
        AddMergedRow(sheet, new ExcelStyledCell(legend, Legend));
        sheet.AddRow();
    }

    // Two navy cells across the width: on the statement's 10 columns the left block is A–F and
    // the right G–J, and narrower sheets keep the same four-column right block.
    private static void AddSplitBandRow(ExcelSheet sheet, ExcelStyledCell left, ExcelStyledCell right)
    {
        var count = sheet.Columns.Count;
        var rightStart = Math.Max(count - 4, 1);
        var cells = Enumerable.Repeat<object?>(new ExcelStyledCell(null, Band), count).ToArray();
        cells[0] = left;
        cells[rightStart] = right;
        sheet.AddRow(cells);
        sheet.MergedRanges.Add($"A{sheet.Rows.Count}:{ColumnLetter(rightStart - 1)}{sheet.Rows.Count}");
        sheet.MergedRanges.Add($"{ColumnLetter(rightStart)}{sheet.Rows.Count}:{ColumnLetter(count - 1)}{sheet.Rows.Count}");
    }

    // One cell of text merged across the width — the legend and working-copy lines.
    private static void AddMergedRow(ExcelSheet sheet, ExcelStyledCell first)
    {
        sheet.AddRow(first);
        sheet.MergedRanges.Add($"A{sheet.Rows.Count}:{ColumnLetter(sheet.Columns.Count - 1)}{sheet.Rows.Count}");
    }

    // Export sheets never pass 26 columns, so single letters suffice.
    private static string ColumnLetter(int index) => ((char)('A' + index)).ToString();
}
