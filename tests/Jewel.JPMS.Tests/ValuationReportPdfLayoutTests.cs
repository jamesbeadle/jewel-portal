using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using UglyToad.PdfPig;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The bill on the valuation report PDF prints every figure on one line. The accountant's
/// 2026-09-02 export had negatives printing as a bare "-" with the pounds on the line below:
/// MigraDoc breaks after a hyphen that is not followed by a digit, and the columns were a
/// hair too narrow for a six-figure negative in the host's DejaVu Sans. These tests render
/// with that face (Fonts/, via RequestDocuments__FontPath) and read the PDF back, so the
/// widths they prove are the widths production prints.
/// </summary>
public sealed class ValuationReportPdfLayoutTests
{
    static ValuationReportPdfLayoutTests()
    {
        // The resolver reads the override once, on the first render anywhere in the process —
        // set it before that. Should another class render first, the face assertion below says so.
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fonts");
        if (Directory.Exists(fonts))
            Environment.SetEnvironmentVariable("RequestDocuments__FontPath", fonts);
    }

    [Fact]
    public void NegativeMoney_carriesAnUnbreakableMinusSign()
    {
        var text = ValuationReportSnapshotRenderer.Money(-10_573.80m);

        Assert.Equal("−£10,573.80", text);
        Assert.DoesNotContain('-', text);
        Assert.Equal("£10,573.80", ValuationReportSnapshotRenderer.Money(10_573.80m));
    }

    // The figures the accountant's export tripped on, and the widest a statement can plausibly
    // carry: a six-figure negative on a variation line, and a seven-figure bold total.
    [Fact]
    public void BillFigures_printOnOneLine_inTheHostFont()
    {
        var lines = new[]
        {
            Line(1, ValuationElementType.ContractWorks, ValuationLineType.Priced, "Main contract works", 1_234_567.89m, 100m, 1_234_567.89m, 0m),
            Line(2, ValuationElementType.Variation, ValuationLineType.Omit, "Plumbing & Heating — tender value (omit)", -117_223.37m, 100m, -117_223.37m, -28_525.44m, variationRef: "V17", variationTitle: "Plumbing & Heating"),
            Line(3, ValuationElementType.Variation, ValuationLineType.Priced, "Fluid Glazing — balance paid direct by client", -28_525.44m, 100m, -28_525.44m, 0m, variationRef: "V27", variationTitle: "Fluid Glazing"),
        };
        var document = new ValuationReportSnapshotDocument(
            "JBB-2026-004", "Woodhouse", "David Needham", new ValuationReportSnapshotDetail(Snapshot(lines), lines));

        var pdf = ValuationReportSnapshotRenderer.Render(document);

        using var reader = PdfDocument.Open(pdf);
        var words = reader.GetPages().SelectMany(page => page.GetWords()).ToList();
        var texts = words.Select(word => word.Text).ToList();

        // Rendered with production's face, or this test is not proving production's widths.
        var faces = words.SelectMany(word => word.Letters).Select(letter => letter.FontName).Distinct().ToList();
        Assert.Contains(faces, face => face.Contains("DejaVu", StringComparison.OrdinalIgnoreCase));

        // Each figure is one word — its sign attached, nothing spilt onto a second line.
        Assert.Contains("−£117,223.37", texts);   // the V17 line and the Variations total
        Assert.Contains("−£28,525.44", texts);    // the V27 line
        Assert.Contains("£1,234,567.89", texts);       // the Contract Works line and its bold total
        Assert.DoesNotContain(texts, text => text is "-" or "−" or "£");

        // And no figure was broken at its digits either: every money word we printed is whole.
        var money = texts.Where(text => text.Contains('£')).ToList();
        Assert.All(money, text => Assert.Matches(@"^−?£\d{1,3}(,\d{3})*\.\d{2}$", text));
    }

    private static ValuationReportSnapshotLine Line(
        int order, ValuationElementType element, ValuationLineType type, string description,
        decimal lineAmount, decimal percent, decimal cumulative, decimal period,
        string variationRef = "", string variationTitle = "") =>
        new(
            ValuationReportSnapshotLineId: $"SL{order}",
            ValuationReportSnapshotId: "SNAP-1",
            SourceValuationLineItemId: $"L{order}",
            ElementType: element,
            SectionCode: element == ValuationElementType.Variation ? "" : "MAIN",
            SectionName: element == ValuationElementType.Variation ? "" : "Main works",
            VariationRef: variationRef,
            VariationTitle: variationTitle,
            LineType: type,
            CostCode: "SUB-GWK",
            Description: description,
            Unit: "item",
            Quantity: 1m,
            Rate: lineAmount,
            LineAmount: lineAmount,
            PercentComplete: percent,
            CumulativeClaimed: cumulative,
            PeriodIncrement: period,
            Comments: "",
            DisplayOrder: order);

    private static ValuationReportSnapshot Snapshot(IReadOnlyList<ValuationReportSnapshotLine> lines)
    {
        var contractSum = lines.Where(l => l.ElementType != ValuationElementType.Variation && l.CountsTowardTotals).Sum(l => l.LineAmount);
        var netVariations = lines.Where(l => l.ElementType == ValuationElementType.Variation && l.CountsTowardTotals).Sum(l => l.LineAmount);
        var worksComplete = lines.Where(l => l.CountsTowardTotals).Sum(l => l.CumulativeClaimed);
        var retention = ValuationCalculations.RetentionHeld(worksComplete, 5m);
        return new ValuationReportSnapshot(
            "SNAP-1", "P1", null, null, "August 2026 — working copy", new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero), false,
            contractSum, netVariations, ValuationCalculations.RevisedContractSum(contractSum, netVariations),
            worksComplete, 5m, retention, 0m, 0m, 266_679.55m,
            ValuationCalculations.PaymentDueExVat(worksComplete, retention, 0m, 0m, 266_679.55m));
    }
}
