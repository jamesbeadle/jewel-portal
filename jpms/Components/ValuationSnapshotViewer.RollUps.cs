using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Components;

// The consolidated variation rows of a frozen snapshot (VariationRollUps): one row per variation
// order per cost centre, with the frozen lines reachable beneath it on screen. The client's PDF
// and the workbook's Summary tab consolidate one level further, to the whole variation order.
// Every figure is a sum of what was frozen; nothing is recomputed.
public partial class ValuationSnapshotViewer
{
    private sealed record BillRow(ValuationReportSnapshotLine? Line, VariationRollUp<ValuationReportSnapshotLine>? RollUp, bool IsDetail);

    private readonly HashSet<string> openRollUps = new();

    private IEnumerable<BillRow> RowsFor(Section section)
    {
        if (section.Type != ValuationElementType.Variation)
            return section.Lines.Select(line => new BillRow(line, null, false));
        return VariationRollUps.Build(section.Lines).SelectMany(RowsFor);
    }

    private IEnumerable<BillRow> RowsFor(VariationRollUp<ValuationReportSnapshotLine> rollUp)
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

    private bool IsRollUpOpen(VariationRollUp<ValuationReportSnapshotLine> rollUp) => openRollUps.Contains(rollUp.Key);

    private void ToggleRollUp(VariationRollUp<ValuationReportSnapshotLine> rollUp)
    {
        if (!openRollUps.Remove(rollUp.Key)) openRollUps.Add(rollUp.Key);
    }

    private string RollUpSubtitle(VariationRollUp<ValuationReportSnapshotLine> rollUp)
    {
        var centre = CostCentreNameFor(rollUp.CostCode) ?? rollUp.CostCode;
        return $"{rollUp.Lines.Count} lines consolidated · {centre}";
    }

    private static decimal RollUpClaimed(VariationRollUp<ValuationReportSnapshotLine> rollUp) =>
        rollUp.CountingLines.Sum(line => line.CumulativeClaimed);

    private static decimal RollUpPercent(VariationRollUp<ValuationReportSnapshotLine> rollUp) =>
        VariationRollUps.WeightedPercent(RollUpClaimed(rollUp), rollUp.Amount);
}
