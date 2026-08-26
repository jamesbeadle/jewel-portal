namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The coarser consolidation the client-facing statement and the workbook's Summary tab use:
/// one row per variation order, every cost centre it touches folded into it. The screens keep
/// the finer per-cost-centre grouping (<see cref="VariationRollUps"/>) because that is where a
/// percentage is set; the statement shows the order's total because one line reads cleaner.
/// A roll-up whose lines all sit in one cost centre keeps that centre's code; a mixed one
/// carries none rather than one centre's code posing as the order's.
/// </summary>
public static class VariationOrderRollUps
{
    public static IReadOnlyList<VariationRollUp<TLine>> Build<TLine>(IEnumerable<TLine> variationLines)
        where TLine : IVariationBillLine =>
        variationLines
            .GroupBy(line => KeyFor(line.VariationRef))
            .Select(group => RollUpFrom(group.OrderBy(line => line.DisplayOrder).ToList()))
            .OrderBy(rollUp => VariationRollUps.VariationRefOrder(rollUp.VariationRef))
            .ThenBy(rollUp => rollUp.Lines[0].DisplayOrder)
            .ToList();

    public static string KeyFor(string variationRef) => variationRef.Trim().ToUpperInvariant();

    private static VariationRollUp<TLine> RollUpFrom<TLine>(IReadOnlyList<TLine> orderedLines)
        where TLine : IVariationBillLine
    {
        var first = orderedLines[0];
        return new VariationRollUp<TLine>(first.VariationRef.Trim(), first.VariationTitle, SharedCostCode(orderedLines), orderedLines);
    }

    private static string SharedCostCode<TLine>(IEnumerable<TLine> lines) where TLine : IVariationBillLine
    {
        var distinct = lines
            .Select(line => line.CostCode.Trim())
            .Where(costCode => costCode.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return distinct.Count == 1 ? distinct[0] : "";
    }
}
