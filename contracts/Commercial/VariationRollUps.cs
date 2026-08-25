using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// What a variation line must expose to be rolled up on the valuation report. Implemented by
/// the live <see cref="ValuationLineItem"/> and the frozen <see cref="ValuationReportSnapshotLine"/>
/// so one grouping rule serves every surface.
/// </summary>
public interface IVariationBillLine
{
    string VariationRef { get; }
    string VariationTitle { get; }
    string CostCode { get; }
    decimal LineAmount { get; }
    int DisplayOrder { get; }
    bool CountsTowardTotals { get; }
}

/// <summary>
/// One row of the Variations section as the report shows it: every line of one variation order
/// allocated to one cost centre, consolidated. A single-line roll-up renders as the line itself;
/// a multi-line roll-up renders as one consolidated row whose % complete is the weighted result
/// of its lines (claimed ÷ amount), with the lines themselves reachable underneath on screen.
/// </summary>
public sealed record VariationRollUp<TLine>(
    string VariationRef,
    string VariationTitle,
    string CostCode,
    IReadOnlyList<TLine> Lines) where TLine : IVariationBillLine
{
    public string Key => VariationRollUps.KeyFor(VariationRef, CostCode);
    public bool IsRolledUp => Lines.Count > 1;
    public bool CountsTowardTotals => Lines.Any(line => line.CountsTowardTotals);
    public IEnumerable<TLine> CountingLines => Lines.Where(line => line.CountsTowardTotals);
    public decimal Amount => CountingLines.Sum(line => line.LineAmount);
}

/// <summary>
/// The shared rule for consolidating variation lines: group by variation reference and cost
/// centre, variations in their natural numeric order, groups and lines within them in display
/// order. Nothing here touches a claim — percentages are derived by each surface from the
/// entries it holds, through <see cref="WeightedPercent"/>.
/// </summary>
public static class VariationRollUps
{
    private const decimal WholePercent = 100m;
    private const int PercentDecimalPlaces = 2;

    public static string KeyFor(string variationRef, string costCode) =>
        $"{variationRef.Trim().ToUpperInvariant()}|{costCode.Trim().ToUpperInvariant()}";

    public static IReadOnlyList<VariationRollUp<TLine>> Build<TLine>(IEnumerable<TLine> variationLines)
        where TLine : IVariationBillLine =>
        variationLines
            .GroupBy(line => KeyFor(line.VariationRef, line.CostCode))
            .Select(group => RollUpFrom(group.OrderBy(line => line.DisplayOrder).ToList()))
            .OrderBy(rollUp => VariationRefOrder(rollUp.VariationRef))
            .ThenBy(rollUp => rollUp.Lines[0].DisplayOrder)
            .ToList();

    private static VariationRollUp<TLine> RollUpFrom<TLine>(IReadOnlyList<TLine> orderedLines)
        where TLine : IVariationBillLine
    {
        var first = orderedLines[0];
        return new VariationRollUp<TLine>(first.VariationRef.Trim(), first.VariationTitle, first.CostCode.Trim(), orderedLines);
    }

    /// <summary>"V18" sorts after "V9": the digits decide, refs without any go last.</summary>
    public static int VariationRefOrder(string variationRef)
    {
        var digits = new string(variationRef.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    /// <summary>
    /// The consolidated % complete: claimed ÷ amount. A roll-up whose lines net to nothing has
    /// no meaningful percentage and reads as 0.
    /// </summary>
    public static decimal WeightedPercent(decimal claimed, decimal amount) =>
        amount == 0m ? 0m : Math.Round(claimed / amount * WholePercent, PercentDecimalPlaces);
}
