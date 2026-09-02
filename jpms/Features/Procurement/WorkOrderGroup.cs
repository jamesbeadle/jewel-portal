namespace Jewel.JPMS.Features.Procurement;

/// <summary>One row of the work-orders table — a cost centre or a supplier, whichever the tab
/// is grouped by — and the same shape summed for its footer. PaidKnown: at least one order behind
/// this row has a payment position JPMS can stand behind (a linked bill, a migrated opening
/// balance, or a net-credit order on which no bill is ever due, so £0.00 paid is a fact). When no
/// order in the row does, the row's Paid is not zero — it is unknown — and the table says so with
/// an em dash. KnownCommitted is the committed value on just those known orders: Remaining is
/// KnownCommitted less Paid, so an unlinked order's value is never passed off as wholly unpaid —
/// and the Remaining column always sums to its footer total.</summary>
public sealed record WorkOrderGroup(
    string Key,
    string Code,
    string Name,
    bool InMaster,
    int OrderCount,
    decimal Committed,
    decimal KnownCommitted,
    decimal Paid,
    decimal Invoiced,
    bool PaidKnown)
{
    public decimal Remaining => KnownCommitted - Paid;
    public decimal UnknownCommitted => Committed - KnownCommitted;
}

/// <summary>One priced line of one order — the unit the table groups and sums.</summary>
public sealed record WorkOrderLineEntry(ProjectWorkOrderDetail Detail, WorkOrderLine Line);
