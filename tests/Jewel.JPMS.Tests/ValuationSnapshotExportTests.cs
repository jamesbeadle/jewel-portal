using ClosedXML.Excel;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial.Export;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The snapshot → workbook mapping is shared by the portal's Export button and the connector's
/// export_valuation_report (2026-09-02), so the spreadsheet is the same file whichever way it
/// is fetched. These tests pin the mapping and prove the shared writer produces a workbook
/// Excel opens, with the tabs the accountant expects.
/// </summary>
public sealed class ValuationSnapshotExportTests
{
    [Fact]
    public void ALineMapsWithItsPreviousDerivedFromTheFrozenMovement()
    {
        var line = Line(1, ValuationElementType.ContractWorks, "Roof covering", lineAmount: 9_610m, percent: 100m, cumulative: 9_610m, period: 2_000m);

        var mapped = ValuationSnapshotExport.LineFor("Contract Works", line, _ => "Roofer");

        Assert.Equal("Contract Works", mapped.Section);
        Assert.Equal("Roofer", mapped.Area);           // no estimate section on the line → the cost centre's name
        Assert.Equal("ROOF-RFR", mapped.Code);
        Assert.Equal("Roof covering", mapped.Title);
        Assert.Equal("Priced", mapped.LineTypeLabel);
        Assert.Equal(7_610m, mapped.PreviousClaimed);  // cumulative less this period's movement
        Assert.Equal(2_000m, mapped.ThisPeriod);
        Assert.Equal(9_610m, mapped.CumulativeClaimed);
        Assert.True(mapped.MovedThisPeriod);
    }

    [Fact]
    public void AVariationLineLeadsWithItsOwnDescription_andPaddedRef()
    {
        var line = Line(2, ValuationElementType.Variation, "", lineAmount: -28_525.44m, percent: 100m, cumulative: -28_525.44m, period: 0m,
            variationRef: "V27", variationTitle: "Fluid Glazing — balance paid direct by client");

        var mapped = ValuationSnapshotExport.LineFor("Variations", line, _ => null);

        Assert.Equal("V27", mapped.Code);
        Assert.Equal("Fluid Glazing — balance paid direct by client", mapped.Title);
        Assert.Equal("", mapped.Area);                  // variations never group by area
        Assert.True(mapped.IsVariation);
    }

    [Fact]
    public void LinesComeOutInStatementOrder()
    {
        var lines = new[]
        {
            Line(1, ValuationElementType.Variation, "V", 100m, 0m, 0m, 0m, variationRef: "V01"),
            Line(2, ValuationElementType.Contingency, "C", 100m, 0m, 0m, 0m),
            Line(3, ValuationElementType.ContractWorks, "B", 100m, 0m, 0m, 0m),
            Line(4, ValuationElementType.ContractWorks, "A", 100m, 0m, 0m, 0m, displayOrder: 0),
            Line(5, ValuationElementType.PcSum, "P", 100m, 0m, 0m, 0m),
        };

        var mapped = ValuationSnapshotExport.Lines(lines, _ => null);

        Assert.Equal(new[] { "A", "B", "P", "C", "V" }, mapped.Select(line => line.Title));
        Assert.Equal(new[] { "Contract Works", "Contract Works", "Provisional Sums", "Contingency Sums", "Variations" }, mapped.Select(line => line.Section));
    }

    [Fact]
    public void TheSummaryIsTheSnapshotsOwnFooter()
    {
        var lines = new[] { Line(1, ValuationElementType.ContractWorks, "A", 1_000m, 50m, 500m, 200m) };
        var snapshot = Snapshot(lines);

        var summary = ValuationSnapshotExport.Summary(snapshot, lines);

        Assert.Equal("Original contract sum", summary[0].Label);
        Assert.Equal(1_000m, summary[0].Amount);
        Assert.Equal(200m, summary.Single(row => row.Label == "Works claimed this period").Amount);
        Assert.Equal("Payment due (ex VAT)", summary[^1].Label);
        Assert.True(summary[^1].Strong);
    }

    [Fact]
    public void AWorkingCopyAndAFrozenSnapshotAreStampedDifferently()
    {
        var snapshot = Snapshot(new[] { Line(1, ValuationElementType.ContractWorks, "A", 1_000m, 50m, 500m, 200m) });

        var draft = ValuationSnapshotExport.Meta(snapshot, isDraft: true);
        var frozen = ValuationSnapshotExport.Meta(snapshot, isDraft: false);

        Assert.True(draft.IsDraft);
        Assert.StartsWith("Prepared 02 Sep 2026", draft.PreparedLabel);
        Assert.Contains("working copy", draft.PreparedLabel);
        Assert.False(frozen.IsDraft);
        Assert.StartsWith("Snapshot taken 02 Sep 2026", frozen.PreparedLabel);
        Assert.Contains("immutable record", frozen.PreparedLabel);
    }

    [Fact]
    public void TheSharedWriterProducesAWorkbookExcelOpens_withTheStatementTabs()
    {
        var lines = new[]
        {
            Line(1, ValuationElementType.ContractWorks, "Main works", 1_234_567.89m, 100m, 1_234_567.89m, 0m),
            Line(2, ValuationElementType.Variation, "", -117_223.37m, 100m, -117_223.37m, -28_525.44m, variationRef: "V17", variationTitle: "Plumbing & Heating"),
        };
        var snapshot = Snapshot(lines);
        var workbook = ValuationReportExportWorkbook.Build(
            ValuationSnapshotExport.Meta(snapshot, isDraft: true),
            ValuationSnapshotExport.Lines(lines, _ => null),
            ValuationSnapshotExport.Summary(snapshot, lines),
            pendingVariations: Array.Empty<ValuationExportPendingVariation>());

        var bytes = ExcelWorkbookWriter.Write(workbook);

        using var opened = new XLWorkbook(new MemoryStream(bytes));
        var names = opened.Worksheets.Select(sheet => sheet.Name).ToList();
        Assert.Equal("Summary", names[0]);
        Assert.Contains("V17", names);
        Assert.Contains("Pending variations", names);
        // The Summary tab carries the statement label and the seven-figure line.
        var summaryCells = opened.Worksheet("Summary").CellsUsed().Select(cell => cell.GetFormattedString()).ToList();
        Assert.Contains(summaryCells, text => text.Contains("August 2026 — working copy"));
        Assert.Contains(summaryCells, text => text == "Main works");
        Assert.Contains(opened.Worksheet("Summary").CellsUsed().Select(cell => cell.Value), value => value.IsNumber && value.GetNumber() == 1_234_567.89);
    }

    private static ValuationReportSnapshotLine Line(
        int order, ValuationElementType element, string description,
        decimal lineAmount, decimal percent, decimal cumulative, decimal period,
        string variationRef = "", string variationTitle = "", int? displayOrder = null) =>
        new(
            ValuationReportSnapshotLineId: $"SL{order}",
            ValuationReportSnapshotId: "SNAP-1",
            SourceValuationLineItemId: $"L{order}",
            ElementType: element,
            SectionCode: "",
            SectionName: "",
            VariationRef: variationRef,
            VariationTitle: variationTitle,
            LineType: lineAmount < 0m ? ValuationLineType.Omit : ValuationLineType.Priced,
            CostCode: element == ValuationElementType.Variation ? "MEC-PLM" : "ROOF-RFR",
            Description: description,
            Unit: "item",
            Quantity: 1m,
            Rate: lineAmount,
            LineAmount: lineAmount,
            PercentComplete: percent,
            CumulativeClaimed: cumulative,
            PeriodIncrement: period,
            Comments: "",
            DisplayOrder: displayOrder ?? order);

    private static ValuationReportSnapshot Snapshot(IReadOnlyList<ValuationReportSnapshotLine> lines)
    {
        var contractSum = lines.Where(l => l.ElementType != ValuationElementType.Variation && l.CountsTowardTotals).Sum(l => l.LineAmount);
        var netVariations = lines.Where(l => l.ElementType == ValuationElementType.Variation && l.CountsTowardTotals).Sum(l => l.LineAmount);
        var worksComplete = lines.Where(l => l.CountsTowardTotals).Sum(l => l.CumulativeClaimed);
        var retention = ValuationCalculations.RetentionHeld(worksComplete, 5m);
        return new ValuationReportSnapshot(
            "SNAP-1", "P1", null, null, "August 2026 — working copy", new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero), false,
            contractSum, netVariations, ValuationCalculations.RevisedContractSum(contractSum, netVariations),
            worksComplete, 5m, retention, 0m, 0m, 0m,
            ValuationCalculations.PaymentDueExVat(worksComplete, retention, 0m, 0m, 0m));
    }
}
