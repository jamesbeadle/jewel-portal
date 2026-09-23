using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// The build-up a variation is approved with: its priced lines, their total as the approved
/// value and the first line's cost centre as the primary. The approve panel builds one from the
/// rows a person keys; a staged build-up already IS one, so a move to Approved can take it
/// without the panel.
/// </summary>
public sealed record VariationApproval(string PrimaryCostCode, decimal Total, IReadOnlyList<VariationLineInput> Lines)
{
    /// <summary>
    /// The approval the staged build-up makes on its own — exactly what the panel submits when it
    /// opens pre-seeded from those lines. Null when nothing is staged: the lines have to be entered.
    /// Staging refuses a line without a cost centre and a zero total, so a staged build-up always
    /// passes the panel's own checks.
    /// </summary>
    public static VariationApproval? FromStagedBuildUp(VariationOrder order)
    {
        if (order.DraftLines is not { Count: > 0 } lines) return null;
        var total = lines.Sum(line => line.Quantity * line.Rate);
        return new VariationApproval(lines[0].CostCode, total, lines);
    }
}
