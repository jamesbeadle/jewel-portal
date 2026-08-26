using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// One tab per approved variation order — the detail behind the Summary tab's one-row-per-order
/// consolidation. Each sheet is the order's lines in the statement's own dress (title band, bill
/// columns, total row), so when specific percentages start being claimed on individual lines the
/// accountant reads them here while the Summary row stays the weighted roll-up.
/// </summary>
internal static class ValuationExportVariationSheets
{
    // Excel refuses these characters in a sheet name, and caps the name at 31 characters.
    private static readonly char[] ForbiddenNameChars = { ':', '\\', '/', '?', '*', '[', ']' };
    private const int MaxSheetNameLength = 31;

    public static void Add(ExcelWorkbook workbook, ValuationExportMeta meta, IReadOnlyList<ValuationExportLine> orderedLines)
    {
        var variationLines = orderedLines.Where(line => line.IsVariation).ToList();
        if (variationLines.Count == 0) return;

        // Names already spoken for: the sheets added so far, and the Pending tab still to come.
        var taken = new HashSet<string>(workbook.Sheets.Select(sheet => sheet.Name), StringComparer.OrdinalIgnoreCase)
        {
            ValuationExportPendingSheet.SheetName
        };
        foreach (var rollUp in VariationOrderRollUps.Build(variationLines))
        {
            AddSheet(workbook, meta, rollUp, UniqueName(RefOr(rollUp, "Variation"), taken));
        }
    }

    private static void AddSheet(
        ExcelWorkbook workbook, ValuationExportMeta meta, VariationRollUp<ValuationExportLine> rollUp, string name)
    {
        var sheet = workbook.AddSheet(name, ValuationExportStatementSheet.BillColumns());
        ValuationExportStatementSheet.SetPresentationFlags(sheet);

        ValuationExportTitleBand.Add(sheet, meta,
            "One variation order's lines as they stand on the report — its consolidated row is on the Summary tab · "
            + "Shaded lines moved this period · “This period” is the movement since the previous statement · All figures net of VAT.");
        ValuationExportStatementSheet.AddHeadingRow(sheet, new ExcelStyledCell(HeadingFor(rollUp), SectionHead), SectionHeadFill);
        ValuationExportBillRows.AddColumnHeadings(sheet);
        foreach (var line in rollUp.Lines)
        {
            ValuationExportBillRows.AddLine(sheet, line);
        }
        ValuationExportBillRows.AddSectionTotal(sheet, RefOr(rollUp, "Order"), rollUp.Lines);
    }

    // "V08 — West Valley SD8 + Ridge/Hip …" — same wording the old Detail tab's bands carried.
    private static string HeadingFor(VariationRollUp<ValuationExportLine> rollUp) =>
        string.Join(" — ", new[] { rollUp.VariationRef, rollUp.VariationTitle }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim()));

    // The V-ref names the tab; a line with no ref (legacy data) falls back so the sheet still exists.
    private static string RefOr(VariationRollUp<ValuationExportLine> rollUp, string fallback) =>
        string.IsNullOrWhiteSpace(rollUp.VariationRef) ? fallback : rollUp.VariationRef.Trim();

    private static string UniqueName(string wanted, HashSet<string> taken)
    {
        var cleaned = new string(wanted.Select(c => ForbiddenNameChars.Contains(c) ? ' ' : c).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "Variation";
        if (cleaned.Length > MaxSheetNameLength) cleaned = cleaned[..MaxSheetNameLength].Trim();
        var name = cleaned;
        // Refs are unique per order, so a collision only follows the blank-ref fallback.
        for (var n = 2; !taken.Add(name); n++)
        {
            var suffix = $" ({n})";
            name = cleaned[..Math.Min(cleaned.Length, MaxSheetNameLength - suffix.Length)].Trim() + suffix;
        }
        return name;
    }
}
