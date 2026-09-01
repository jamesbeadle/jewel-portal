using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- Previous claim comparison -----------------------------------------
    // "Prev. %" is the cumulative % complete this line carried on the claim before the selected
    // one; the delta beside the current % is how much is being claimed this period.
    private bool HasPreviousClaim => PreviousClaim is not null;

    private decimal PreviousPercentFor(ValuationLineItem line) =>
        previousEntries.FirstOrDefault(e => e.ValuationLineItemId == line.ValuationLineItemId)?.PercentComplete ?? 0m;

    private decimal DeltaFor(ValuationLineItem line) => PercentFor(line) - PreviousPercentFor(line);

    // Only worth showing a delta when there's a prior claim to compare against, the line is priced
    // into totals, and the percentage actually moved.
    private bool ShowDelta(ValuationLineItem line) =>
        HasPreviousClaim && line.CountsTowardTotals && DeltaFor(line) != 0m;

    private string PrevColumnTitle => PreviousClaim is null
        ? "No earlier claim to compare against — this is the first claim"
        : $"Cumulative % complete claimed on {PreviousClaim.DisplayName}";

    // Movement in money this period: cumulative claimed now less the previous claim's
    // cumulative (both computed against the CURRENT line amount, so a re-priced line
    // doesn't produce a phantom movement). No previous claim → everything is movement.
    private decimal PreviousClaimedFor(ValuationLineItem line) =>
        ValuationCalculations.CumulativeClaimed(PreviousPercentFor(line), line.LineAmount);

    private decimal PeriodFor(ValuationLineItem line) => ClaimedFor(line) - PreviousClaimedFor(line);

    private decimal PeriodTotalFor(Section section) =>
        section.Lines.Where(l => l.CountsTowardTotals).Sum(PeriodFor);

    private decimal PeriodClaimedTotal =>
        lines.Where(l => l.CountsTowardTotals).Sum(PeriodFor);

}
