using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Where an UNALLOCATED line sits on the Cost allocation page — the tab it renders in. The page
/// and the connector both partition with this one rule (2026-09-11, the accountant's ask: the
/// connector read 44 unallocated lines against a tab bar that added up to 14, because it could
/// not tell a labour line the settlement run had covered from a line that wanted coding).
/// </summary>
public enum XeroLedgerQueue
{
    /// <summary>The ordinary queue — nothing recognised the line; it wants a project and cost centre.</summary>
    ToCode = 0,
    /// <summary>A labour-registry worker's bill not yet covered by timesheets — the Labour tab's number.</summary>
    Labour = 1,
    /// <summary>A worker's bill already covered by timesheets — nothing to do; the page keeps it behind "show covered".</summary>
    LabourCovered = 2,
    /// <summary>A bill matched to open work order(s) with its figures proposed — one Approve on the Work Order bills tab.</summary>
    WorkOrderBill = 3,
    /// <summary>A Work Order bill the ladder could not place (figures still to key) seen by someone
    /// who may not key them — it stays on the Work Order bills tab for the Finance Director and is
    /// hidden from this viewer entirely; never the plain queue.</summary>
    WorkOrderBillHeldForFinance = 4
}

public static class XeroLedgerQueues
{
    /// <summary>
    /// Who may handle a Work Order bill the ladder could not place — the "figures to set" card
    /// (2026-09-11, the accountant's rule: the owner never sees a money field; the odd one is
    /// the Finance Director's, on the same tab). Admin passes as everywhere.
    /// </summary>
    public static bool MayHandleUnplacedWorkOrderBill(Role role) =>
        role is Role.Admin or Role.FinanceDirector;

    public static bool MayHandleUnplacedWorkOrderBill(IEnumerable<Role> roles) =>
        roles.Any(MayHandleUnplacedWorkOrderBill);

    /// <summary>A match with no figure proposed — the card would ask for them.</summary>
    public static bool IsUnplaced(WorkOrderBillMatch match) =>
        match.Rule == WorkOrderMatchRule.BySupplierOrders;

    /// <summary>
    /// The queue a line belongs to, or null when it is not Unallocated. Labour wins over a
    /// work-order match (a worker's bill is settlement, not a cost); a work-order match wins
    /// over the plain queue; an unplaced match is held for finance unless the viewer may key it.
    /// </summary>
    public static XeroLedgerQueue? Of(XeroLedgerLine line, bool viewerMayHandleUnplaced)
    {
        if (line.AllocationStatus != XeroAllocationStatus.Unallocated) return null;
        if (line.MatchedWorkerId is not null || line.CoveredByTimesheets)
            return line.CoveredByTimesheets ? XeroLedgerQueue.LabourCovered : XeroLedgerQueue.Labour;
        if (line.WorkOrderMatch is not null)
            return IsUnplaced(line.WorkOrderMatch) && !viewerMayHandleUnplaced
                ? XeroLedgerQueue.WorkOrderBillHeldForFinance
                : XeroLedgerQueue.WorkOrderBill;
        return XeroLedgerQueue.ToCode;
    }
}
