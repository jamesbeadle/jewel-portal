using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// What each work order on a project has actually been PAID, read from the Xero bills linked
/// to it rather than from a stored number.
///
/// WorkOrderLines.PaidToDate is a Buildertrend OPENING BALANCE — hand-typed into the seed
/// scripts when the projects were migrated, and never written since by anything except the
/// re-code apportionment. Nothing in JPMS has ever moved it in response to a payment, which is
/// why an order raised in JPMS reads zero paid however many of its bills Xero has settled: the
/// invoiced side of the same page is live off XeroLineWorkOrderLinks, and the paid side was not.
///
/// The rule here is deliberately "links win, opening balance is the fallback", NOT the sum of
/// the two. On a seeded order the two describe the SAME money: Capital Piling's seeded
/// GBP 27,448 is a hand-typed snapshot of exactly the bills that are now linked and settled in
/// Xero, so adding them would show GBP 54,896 paid against a GBP 27,448 order. An order with no
/// linked purchase line at all has nothing better than its opening balance, so it keeps it —
/// which also leaves projects that have yet to be linked up reading exactly as they do today.
/// </summary>
internal static class WorkOrderPaidPositions
{
    /// <summary>
    /// Paid-from-Xero per work order for one project. Only orders with at least one linked
    /// purchase line appear: an absent order is one the ledger knows nothing about, and its
    /// caller keeps the stored opening balance for it.
    /// </summary>
    public static async Task<Dictionary<string, decimal>> ForProjectAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        // Inner join on purpose — a slice whose ledger line has gone (the bill was voided or
        // deleted, and sync removed the line) is not a payment and must not hold a stale
        // position open. Slices only ever exist on whole-line allocations to this project.
        var slices = await context.XeroLineWorkOrderLinks.AsNoTracking()
            .Where(link => link.ProjectId == projectId)
            .Join(context.XeroLedgerLines.AsNoTracking(),
                link => link.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (link, line) => new
                {
                    link.WorkOrderId,
                    link.Amount,
                    line.InvoiceStatus,
                    line.InvoiceTotal,
                    line.AmountDue
                })
            .ToListAsync(cancellationToken);

        return slices
            .GroupBy(slice => slice.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(slice => XeroPaymentMaths.PaidPartOfSlice(
                    slice.Amount, slice.InvoiceStatus, slice.InvoiceTotal, slice.AmountDue)),
                StringComparer.OrdinalIgnoreCase);
    }
}
