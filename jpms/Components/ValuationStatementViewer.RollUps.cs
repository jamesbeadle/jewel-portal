
namespace Jewel.JPMS.Components;

// The consolidated variation rows of a valuation statement (VariationRollUps): one row per variation
// order per cost centre, with the frozen lines reachable beneath it on screen. The client's PDF
// and the workbook's Summary tab consolidate one level further, to the whole variation order.
// Every figure is a sum of the statement's lines; nothing is recomputed.
public partial class ValuationStatementViewer
{
    private sealed record BillRow(ValuationStatementLine? Line, VariationRollUp<ValuationStatementLine>? RollUp, bool IsDetail);

    private readonly HashSet<string> openRollUps = new();

    private IEnumerable<BillRow> RowsFor(Section section)
    {
        if (section.Type != ValuationElementType.Variation)
            return section.Lines.Select(line => new BillRow(line, null, false));
        return VariationRollUps.Build(section.Lines).SelectMany(RowsFor);
    }

    private IEnumerable<BillRow> RowsFor(VariationRollUp<ValuationStatementLine> rollUp)
    {
        if (!rollUp.IsRolledUp)
        {
            yield return new BillRow(rollUp.Lines[0], null, false);
            yield break;
        }
        yield return new BillRow(null, rollUp, false);
        if (!IsRollUpOpen(rollUp)) yield break;
        foreach (var line in rollUp.Lines)
            yield return new BillRow(line, null, true);
    }

    private bool IsRollUpOpen(VariationRollUp<ValuationStatementLine> rollUp) => openRollUps.Contains(rollUp.Key);

    private void ToggleRollUp(VariationRollUp<ValuationStatementLine> rollUp)
    {
        if (!openRollUps.Remove(rollUp.Key)) openRollUps.Add(rollUp.Key);
    }

    private string RollUpSubtitle(VariationRollUp<ValuationStatementLine> rollUp)
    {
        var centre = CostCentreNameFor(rollUp.CostCode) ?? rollUp.CostCode;
        return $"{rollUp.Lines.Count} lines consolidated · {centre}";
    }

    private static decimal RollUpClaimed(VariationRollUp<ValuationStatementLine> rollUp) =>
        rollUp.CountingLines.Sum(line => line.CumulativeClaimed);

    private static decimal RollUpPercent(VariationRollUp<ValuationStatementLine> rollUp) =>
        VariationRollUps.WeightedPercent(RollUpClaimed(rollUp), rollUp.Amount);
}
