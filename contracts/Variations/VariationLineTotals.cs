namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// The one wording for a variation build-up whose lines total zero. A blank cell on a
/// workbook's summary sheet is where a zero total usually comes from, so the refusal says
/// where the figures normally are rather than only what is wrong with the ones sent.
/// </summary>
public static class VariationLineTotals
{
    public const string ZeroTotalMessage =
        "The lines' total can't be zero — enter the agreed values (a negative rate for an omit). "
        + "If you read the value off a summary sheet and found it blank, the figures are usually on the "
        + "item's own tab or sheet; read that before treating the variation as nil.";
}
