using static Jewel.JPMS.Features.Cvr.ProfitDisplay;

namespace Jewel.JPMS.Features.Cvr;

/// <summary>The memo lines under the profit table's money figures — one wording, read by the
/// table's rows, its totals row and the phone's cards alike. Null is "no memo".</summary>
public static class ProfitMemos
{
    public static string? RetentionHeld(ProfitRow row) =>
        row.RetentionOutstanding > 0m ? $"{Money(row.RetentionOutstanding)} retention held" : null;

    public static string? Variations(ProfitRow row) =>
        row.NetVariations == 0m ? "no variations" : $"{SignedMoney(row.NetVariations)} variations";

    /// <summary>The selection's variations — silent at nil, where a project row says "no variations".</summary>
    public static string? VariationsOfTotal(ProfitRow total) =>
        total.NetVariations == 0m ? null : Variations(total);

    public static string? CostMovementAgainstTarget(ProfitRow total) =>
        total.CostMovement < 0m ? $"{SignedMoney(-total.CostMovement)} vs target" : null;
}
