using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- Excel ------------------------------------------------------------------
    private void AddRollUpExportRow(ExcelSheet sheet, VariationRollUp<ValuationLineItem> rollUp) =>
        sheet.AddRow(
            SectionLabel(ValuationElementType.Variation),
            null,
            rollUp.VariationRef,
            $"{rollUp.VariationTitle} ({rollUp.Lines.Count} lines)",
            CostCentreLabel(rollUp.CostCode),
            null,
            null,
            rollUp.Amount,
            rollUp.CountsTowardTotals ? (decimal?)(RollUpPercent(rollUp) / 100m) : null,
            rollUp.CountsTowardTotals ? (decimal?)RollUpClaimed(rollUp) : null);

    // Every variation line with its own %, so the consolidated rows above can be traced.
    private void AddVariationLinesSheet(ExcelWorkbook workbook)
    {
        var variationLines = lines.Where(line => line.ElementType == ValuationElementType.Variation).ToList();
        if (variationLines.Count == 0) return;
        var sheet = workbook.AddSheet("Variation lines",
            new ExcelColumn("Variation"),
            new ExcelColumn("Cost centre"),
            new ExcelColumn("Description"),
            new ExcelColumn("Qty", ExcelFormat.Number),
            new ExcelColumn("Rate £", ExcelFormat.Currency),
            new ExcelColumn("Amount £", ExcelFormat.Currency),
            new ExcelColumn("% complete", ExcelFormat.Percent),
            new ExcelColumn("Claimed £", ExcelFormat.Currency));
        foreach (var rollUp in VariationRollUps.Build(variationLines))
            foreach (var line in rollUp.Lines)
                sheet.AddRow(rollUp.VariationRef, CostCentreLabel(rollUp.CostCode), TitleFor(line),
                    line.Quantity, line.Rate, line.LineAmount,
                    line.CountsTowardTotals ? (decimal?)(PercentFor(line) / 100m) : null,
                    line.CountsTowardTotals ? (decimal?)ClaimedFor(line) : null);
    }
}
