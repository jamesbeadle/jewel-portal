using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>The navy title band at the top of a statement tab, with the working-copy stamp and the legend line under it.</summary>
internal static class ValuationExportTitleBand
{
    public const int ColumnCount = 10;
    private const string LastColumn = "J";
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

    // Two navy cells across the width: the left block A–F, the right block G–J.
    private static void AddSplitBandRow(ExcelSheet sheet, ExcelStyledCell left, ExcelStyledCell right)
    {
        var cells = Enumerable.Repeat<object?>(new ExcelStyledCell(null, Band), ColumnCount).ToArray();
        cells[0] = left;
        cells[6] = right;
        sheet.AddRow(cells);
        sheet.MergedRanges.Add($"A{sheet.Rows.Count}:F{sheet.Rows.Count}");
        sheet.MergedRanges.Add($"G{sheet.Rows.Count}:{LastColumn}{sheet.Rows.Count}");
    }

    // One cell of text merged across the width — the legend and working-copy lines.
    private static void AddMergedRow(ExcelSheet sheet, ExcelStyledCell first)
    {
        sheet.AddRow(first);
        sheet.MergedRanges.Add($"A{sheet.Rows.Count}:{LastColumn}{sheet.Rows.Count}");
    }
}
