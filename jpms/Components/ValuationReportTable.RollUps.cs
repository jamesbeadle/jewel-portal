using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

// The consolidated variation rows of the live report (VariationRollUps): one row per variation
// order per cost centre, its % complete the weighted result of the lines beneath it. Typing a %
// on the consolidated row sets every line in it (ValuationReportTable.RollUpEdit.cs); opening the
// row lets each line be set on its own, after which the row's % is what those lines add up to.
public partial class ValuationReportTable
{
    // One rendered row of a bill section: a line, or a consolidated variation row standing in
    // for several. A detail row is a line shown beneath the consolidated row it belongs to.
    private sealed record BillRow(ValuationLineItem? Line, VariationRollUp<ValuationLineItem>? RollUp, bool IsDetail);

    private readonly HashSet<string> openRollUps = new();
    private string? editingRollUpKey;

    private IEnumerable<BillRow> RowsFor(Section section)
    {
        if (section.Type != ValuationElementType.Variation)
            return section.Lines.Select(line => new BillRow(line, null, false));
        return VariationRollUps.Build(section.Lines).SelectMany(RowsFor);
    }

    private IEnumerable<BillRow> RowsFor(VariationRollUp<ValuationLineItem> rollUp)
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

    private bool IsRollUpOpen(VariationRollUp<ValuationLineItem> rollUp) => openRollUps.Contains(rollUp.Key);

    private void ToggleRollUp(VariationRollUp<ValuationLineItem> rollUp)
    {
        if (!openRollUps.Remove(rollUp.Key)) openRollUps.Add(rollUp.Key);
    }

    private void OpenEveryRollUp()
    {
        foreach (var rollUp in VariationRollUps.Build(lines.Where(line => line.ElementType == ValuationElementType.Variation)))
            openRollUps.Add(rollUp.Key);
    }

    private void RevealRollUpFor(ValuationLineItem line)
    {
        if (line.ElementType != ValuationElementType.Variation) return;
        openRollUps.Add(VariationRollUps.KeyFor(line.VariationRef, line.CostCode));
    }

    // ---- Consolidated figures ------------------------------------------------
    private decimal RollUpClaimed(VariationRollUp<ValuationLineItem> rollUp) => rollUp.CountingLines.Sum(ClaimedFor);

    private decimal RollUpPercent(VariationRollUp<ValuationLineItem> rollUp) =>
        VariationRollUps.WeightedPercent(RollUpClaimed(rollUp), rollUp.Amount);

    private decimal RollUpPreviousPercent(VariationRollUp<ValuationLineItem> rollUp) =>
        VariationRollUps.WeightedPercent(rollUp.CountingLines.Sum(PreviousClaimedFor), rollUp.Amount);

    private decimal RollUpPeriod(VariationRollUp<ValuationLineItem> rollUp) => rollUp.CountingLines.Sum(PeriodFor);

    private decimal RollUpDelta(VariationRollUp<ValuationLineItem> rollUp) =>
        RollUpPercent(rollUp) - RollUpPreviousPercent(rollUp);

    private bool ShowRollUpDelta(VariationRollUp<ValuationLineItem> rollUp) =>
        HasPreviousClaim && rollUp.CountsTowardTotals && RollUpDelta(rollUp) != 0m;
}
