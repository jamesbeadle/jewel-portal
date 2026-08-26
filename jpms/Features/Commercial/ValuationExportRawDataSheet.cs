using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>Every exported line as one flat, filterable table — for pivoting and reconciliation.</summary>
internal static class ValuationExportRawDataSheet
{
    private const decimal WholePercent = 100m;

    public static void Add(ExcelWorkbook workbook, IReadOnlyList<ValuationExportLine> lines)
    {
        var sheet = workbook.AddSheet("Raw data",
            new ExcelColumn("Section"),
            new ExcelColumn("Area"),
            new ExcelColumn("Variation"),
            new ExcelColumn("Variation title", Width: 32),
            new ExcelColumn("Cost centre"),
            new ExcelColumn("Code"),
            new ExcelColumn("Description", Width: 44),
            new ExcelColumn("Line type"),
            new ExcelColumn("Unit"),
            new ExcelColumn("Qty", ExcelFormat.Number),
            new ExcelColumn("Rate £", ExcelFormat.Currency),
            new ExcelColumn("Amount £", ExcelFormat.Currency),
            new ExcelColumn("% Complete", ExcelFormat.Percent),
            new ExcelColumn("Previous claimed £", ExcelFormat.Currency),
            new ExcelColumn("This period £", ExcelFormat.Currency),
            new ExcelColumn("Claimed £", ExcelFormat.Currency),
            new ExcelColumn("Moved this period"),
            new ExcelColumn("Counts toward totals"),
            new ExcelColumn("Comments", Width: 36));

        foreach (var line in lines)
        {
            sheet.AddRow(
                line.Section,
                line.Area,
                line.VariationRef,
                line.VariationTitle,
                line.CostCode,
                line.Code,
                line.Title,
                line.LineTypeLabel,
                line.Unit,
                line.Quantity,
                line.Rate,
                line.LineAmount,
                ClaimValue(line, line.PercentComplete / WholePercent),
                ClaimValue(line, line.PreviousClaimed),
                ClaimValue(line, line.ThisPeriod),
                ClaimValue(line, line.CumulativeClaimed),
                line.MovedThisPeriod,
                line.CountsTowardTotals,
                line.Comments);
        }
    }

    private static decimal? ClaimValue(ValuationExportLine line, decimal value) =>
        line.CountsTowardTotals ? value : null;
}
