using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// The Cashflow statement's shared maths, used by both the project Cashflow tab
// (ProjectCashflow.razor) and the company Cash summary (CashSummary.razor) so the two
// can never disagree. Pure functions, no EF/HTTP, unit-tested directly.
public static class CashflowMaths
{
    private const decimal WholePercent = 100m;

    // Retention that will still be withheld on the works left to value: the retention percent
    // applied to the claim value not yet complete. Valuation invoices are raised net of
    // retention, so this slice of the remainder is never invoiceable on a valuation — it comes
    // back through the release rows instead. Deducting it here is what stops the statement
    // counting it twice (once inside left to claim, again inside the forecast releases).
    //
    // Anchored to works complete, not retention outstanding: a confirmed release reduces what
    // is outstanding but changes nothing about what future valuations will withhold.
    // Never negative — works valued beyond the claim (shouldn't happen) must not add money back.
    public static decimal RetentionStillToWithhold(
        decimal projectClaim, decimal totalWorksComplete, decimal retentionPercent) =>
        Math.Max(0m, (projectClaim - totalWorksComplete) * retentionPercent / WholePercent);

    // Left to claim, net of retention: the project claim less cash allocated (cash received
    // plus retention outstanding) less the retention still to be withheld — what can actually
    // be invoiced to the client between now and the releases.
    public static decimal LeftToClaim(
        decimal projectClaim, decimal cashReceived, decimal retentionOutstanding,
        decimal retentionStillToWithhold) =>
        projectClaim - cashReceived - retentionOutstanding - retentionStillToWithhold;

    // The potential upside sitting under the statement: variation orders not yet decided —
    // quoting, issued, or awaiting an Architect's Instruction — at their estimated values.
    // Approved variations are already inside the project claim (their value is in the
    // valuation); rejected ones are gone. An estimate not yet entered contributes nothing.
    public static decimal PotentialVariationValue(IEnumerable<VariationOrder> orders) =>
        orders.Where(order => order.Status.IsPreApproval()).Sum(order => order.EstimatedValue ?? 0m);
}
