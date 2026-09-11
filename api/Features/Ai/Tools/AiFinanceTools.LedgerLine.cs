using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The ledger line as list_xero_ledger_lines hands it to the model: trimmed to what an
/// allocation decision needs (the full record carries sync bookkeeping the model never uses),
/// plus — since 2026-09-09 — the Work Order bill facts the allocation page's Work Order bills
/// tab shows on its card, so approving a supplier's bill against its order(s) is one
/// approve_work_order_bill call from here, exactly as it is one press there; and — since
/// 2026-09-11, the accountant's finding — the QUEUE the page would show the line in
/// (<see cref="XeroLedgerQueues"/>, the page's own partition) with the labour facts behind it,
/// so a worker's bill the settlement run has covered never reads as a line wanting coding.
/// </summary>
internal static partial class AiFinanceTools
{
    private static object Line(XeroLedgerLine line, bool viewerMayHandleUnplaced) => new
    {
        line.XeroLedgerLineId,
        line.XeroInvoiceId,
        line.Type,
        line.InvoiceNumber,
        line.Reference,
        line.ContactName,
        line.Date,
        line.Description,
        line.Net,
        line.AccountCode,
        line.AccountName,
        status = line.AllocationStatus.ToString(),
        queue = XeroLedgerQueues.Of(line, viewerMayHandleUnplaced)?.ToString(),
        projectTab = XeroLedgerQueues.Of(line, viewerMayHandleUnplaced) == XeroLedgerQueue.ToCode
            ? line.ProjectId ?? line.SuggestedProjectId
            : null,
        line.ProjectId,
        line.CostCenterCode,
        line.Bucket,
        line.SuggestedProjectId,
        line.SuggestedCostCenterCode,
        line.SuggestedBucket,
        line.Note,
        splits = line.Splits,
        xeroStatus = line.InvoiceStatus,
        writeBackStatus = line.WriteBackStatus.ToString(),
        line.WriteBackError,
        line.WriteBackFailedAtUtc,
        labour = line.MatchedWorkerId is null && !line.CoveredByTimesheets ? null : new
        {
            workerId = line.MatchedWorkerId,
            workerName = line.MatchedWorkerName,
            subcontractorId = line.MatchedSubcontractorId,
            line.CoveredByTimesheets,
            coveredMonth = line.CoveredPeriodStart?.ToString("yyyy-MM"),
            verdict = line.CoveredByTimesheets
                ? "Settled — an approved timesheet is the actual and this bill is its settlement; nothing to code."
                : "Labour tab — a worker's bill awaiting the settlement run, not a cost to code."
        },
        // The Work Order bills card. For a viewer who may not key figures (queue
        // WorkOrderBillHeldForFinance) the match is withheld: no orders, no figures — the bill is
        // the Finance Director's, and approve_work_order_bill would refuse this role anyway.
        workOrderBill = line.WorkOrderMatch is null
                        || XeroLedgerQueues.Of(line, viewerMayHandleUnplaced) == XeroLedgerQueue.WorkOrderBillHeldForFinance
            ? null
            : WorkOrderBill(line.WorkOrderMatch),
        line.WorkOrderExceptionReason,
        workOrderApproval = line.WorkOrderApproval is null ? null : WorkOrderApproval(line.WorkOrderApproval)
    };

    /// <summary>The card's data: which order(s) matched and why, the split the read proposes,
    /// and every open order of the supplier a slice may go on — with whether Xero tracking can
    /// be written for the proposed split (it cannot when a line would need two values).</summary>
    private static object WorkOrderBill(WorkOrderBillMatch match) => new
    {
        matchedWorkOrderId = match.WorkOrderId,
        matchedWorkOrderReference = match.WorkOrderReference,
        matchedWorkOrderTitle = match.WorkOrderTitle,
        match.ProjectId,
        rule = match.Rule.ToString(),
        match.Detail,
        // Reference beats amount; a conflict is badged, never acted on silently (2026-09-11).
        match.AmountNote,
        proposedSlices = match.ProposedSlices.Select(slice => new
        {
            slice.WorkOrderId,
            reference = match.SupplierOrders.FirstOrDefault(order => order.WorkOrderId == slice.WorkOrderId)?.Reference,
            slice.Net
        }),
        supplierOpenOrders = match.SupplierOrders.Select(order => new
        {
            order.WorkOrderId,
            order.Reference,
            order.Title,
            order.ProjectId,
            order.ProjectName,
            order.OrderValue,
            order.InvoicedToDate,
            order.Remaining,
            order.CostCodes
        }),
        xeroTrackingWritableForProposedSlices = WorkOrderBillTracking.CanBeWritten(
            match.SupplierOrders.Where(order => match.ProposedSlices.Any(slice => slice.WorkOrderId == order.WorkOrderId)))
    };

    private static object WorkOrderApproval(WorkOrderBillApprovalStamp stamp) => new
    {
        orders = stamp.Orders,
        ordersLabel = stamp.OrdersLabel,
        rule = stamp.Rule.ToString(),
        stamp.ApprovedBy,
        stamp.ApprovedAtUtc
    };
}
