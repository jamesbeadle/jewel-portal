using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// The valuation report's summary footer as one value, computed identically for every
// consumer (the Valuation tab footer and the Cashflow tab's retention figures) so the
// two tabs can never disagree. Mirrors the By France workbook summary block.
//
// A Draft claim is computed live from its per-line % complete (certified tracks the
// issued/paid valuation invoices); a locked claim (Preapproved/Confirmed) reads its
// frozen totals; no claim means nothing is being claimed, but invoiced still reads.
public sealed record ValuationSummaryFigures(
    decimal ContractSum,
    decimal NetVariations,
    decimal RevisedContractSum,
    decimal TotalWorksComplete,
    decimal RetentionPercent,
    decimal RetentionHeld,
    decimal RetentionReleasePercent,
    decimal RetentionReleased,
    decimal CertifiedToDate,
    decimal PaymentDueExVat)
{
    // Retention currently withheld by the client — held less what has been released.
    public decimal RetentionOutstanding => RetentionHeld - RetentionReleased;

    public static ValuationSummaryFigures For(
        IReadOnlyList<ValuationLineItem> lines,
        IReadOnlyList<ClaimLine> entries,
        ValuationClaim? claim,
        decimal invoicedToDate)
    {
        var contractSum = ValuationCalculations.ContractSum(lines);
        var netVariations = ValuationCalculations.NetVariations(lines);
        var revisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations);

        var retentionPercent = claim?.RetentionPercent ?? 0m;
        var retentionReleasePercent = claim?.RetentionReleasePercent ?? 0m;

        if (claim is { Status: ValuationClaimStatus.Draft })
        {
            // Live preview for an editable draft. Certified to date tracks the valuation
            // invoices issued so far, so adding/issuing an invoice updates the payment due.
            var totalWorksComplete = lines
                .Where(line => line.CountsTowardTotals)
                .Sum(line => ValuationCalculations.CumulativeClaimed(PercentFor(entries, line), line.LineAmount));
            var retentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, retentionPercent);
            // Retention release is a separate, confirmed event (the Retention tab's "Confirm
            // release"), never part of a claim's payment due: the server freezes RetentionReleased
            // to 0 when the claim locks (ValuationClaimSummary) and the By France report shows
            // £- here. Keep the live draft preview consistent with that so the footer can't add
            // back a forecast release that hasn't happened yet — its forecast lives on the
            // Retention & valuation tab (RetentionSchedule), which counts confirmed releases only.
            const decimal retentionReleased = 0m;
            return new(
                contractSum, netVariations, revisedContractSum, totalWorksComplete,
                retentionPercent, retentionHeld, retentionReleasePercent, retentionReleased,
                invoicedToDate,
                ValuationCalculations.PaymentDueExVat(totalWorksComplete, retentionHeld, retentionReleased, invoicedToDate));
        }

        if (claim is not null)
        {
            // Frozen totals from the locked claim (its CertifiedToDate captured the
            // invoiced total at the moment it was locked).
            return new(
                contractSum, netVariations, revisedContractSum, claim.TotalWorksComplete,
                retentionPercent, claim.RetentionHeld, retentionReleasePercent, claim.RetentionReleased,
                claim.CertifiedToDate, claim.PaymentDueExVat);
        }

        // No claim: nothing is being claimed, but what's been invoiced still reads.
        return new(
            contractSum, netVariations, revisedContractSum, 0m,
            retentionPercent, 0m, retentionReleasePercent, 0m,
            invoicedToDate, 0m);
    }

    private static decimal PercentFor(IReadOnlyList<ClaimLine> entries, ValuationLineItem line) =>
        entries.FirstOrDefault(e => e.ValuationLineItemId == line.ValuationLineItemId)?.PercentComplete ?? 0m;
}
