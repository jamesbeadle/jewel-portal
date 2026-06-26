using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// Pure functions for the valuation report summary maths. Kept free of EF/HTTP so the
// figures can be unit-tested directly against the By France workbook.
public static class ValuationCalculations
{
    private const decimal WholePercent = 100m;

    // qty x rate; an Omit line is always stored as a negative magnitude.
    public static decimal LineAmount(ValuationLineType lineType, decimal quantity, decimal rate)
    {
        var raw = quantity * rate;
        return lineType == ValuationLineType.Omit ? -Math.Abs(raw) : raw;
    }

    // Cumulative amount earned on a line at a given % complete.
    public static decimal CumulativeClaimed(decimal percentComplete, decimal lineAmount) =>
        percentComplete / WholePercent * lineAmount;

    // Original contract sum = priced works + PC sums + contingency (excludes variations,
    // and excludes Declined/TBC lines).
    public static decimal ContractSum(IEnumerable<ValuationLineItem> lines) =>
        lines.Where(line => line.ElementType != ValuationElementType.Variation && line.CountsTowardTotals)
             .Sum(line => line.LineAmount);

    // Net of all variation lines (omits net against additions); Declined/TBC excluded.
    public static decimal NetVariations(IEnumerable<ValuationLineItem> lines) =>
        lines.Where(line => line.ElementType == ValuationElementType.Variation && line.CountsTowardTotals)
             .Sum(line => line.LineAmount);

    public static decimal RevisedContractSum(decimal contractSum, decimal netVariations) =>
        contractSum + netVariations;

    // Sum of cumulative claimed across every priced line in this claim.
    public static decimal TotalWorksComplete(IEnumerable<ClaimLine> claimLines) =>
        claimLines.Sum(line => line.CumulativeClaimed);

    public static decimal RetentionHeld(decimal totalWorksComplete, decimal retentionPercent) =>
        totalWorksComplete * retentionPercent / WholePercent;

    public static decimal RetentionReleased(decimal eligibleWorks, decimal retentionReleasePercent) =>
        eligibleWorks * retentionReleasePercent / WholePercent;

    public static decimal PaymentDueExVat(
        decimal totalWorksComplete,
        decimal retentionHeld,
        decimal retentionReleased,
        decimal certifiedToDate) =>
        totalWorksComplete - retentionHeld + retentionReleased - certifiedToDate;
}
