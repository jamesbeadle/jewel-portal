using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Invoicing progress for every work order on the project: the order's value, the
/// signed net of the Xero purchase lines linked to it (credit notes subtract), and
/// what is left to be invoiced. The single source of truth behind the WO Allocation
/// tab, the Work Orders tab's invoiced figures, and the Financials modal's
/// remaining-balance labels. Ordered by order number.
/// </summary>
public sealed record ListWorkOrderInvoiceSummaries(string ProjectId) : IQuery<IReadOnlyList<WorkOrderInvoiceSummary>>;

/// <summary>
/// Where an order sits against its value. OverInvoiced can only describe links made
/// before the balance check existed — the link command now refuses an allocation
/// that would pass the order's remaining balance.
/// </summary>
public enum WorkOrderInvoicingStatus
{
    NotInvoiced = 0,
    PartInvoiced = 1,
    FullyInvoiced = 2,
    OverInvoiced = 3
}

/// <summary>
/// Where an order sits against its PAYMENT, which is a different question from its invoicing —
/// and one where a bare zero lies. NotLinked is the important case: no purchase line is tied to
/// the order, so JPMS knows nothing about what has been paid, and "GBP 0.00" would read as
/// "nothing has been paid" when it means "we have not been told". Distinguishing the two is the
/// whole point of this enum; the Work Orders tab shows an em dash rather than a figure for it.
///
/// OpeningBalance is its sibling for the migrated orders: nothing linked either, but the order
/// carries a Buildertrend opening balance, so there IS a figure — it is just a carried-over one
/// rather than anything Xero has confirmed.
///
/// NoBillDue is the one case where "nothing linked" is a final answer rather than a pending
/// one: a net-credit order (scope removed, or works the client paid the supplier for
/// directly) will never have a supplier bill to link. Its paid position is a known GBP 0.00,
/// so — unlike NotLinked — its full credit counts in the remaining figure, where it nets off
/// the positive order it was raised against.
/// </summary>
public enum WorkOrderPaymentStatus
{
    NotLinked = 0,
    Unpaid = 1,
    PartPaid = 2,
    Paid = 3,
    OpeningBalance = 4,
    NoBillDue = 5
}

/// <summary>How much an order's paid figure can be trusted. See WorkOrderPaymentStatus.</summary>
public static class WorkOrderPaymentStatuses
{
    /// <summary>
    /// With nothing linked, JPMS has not been told anything about this order's payments: that
    /// is NotLinked, and the reader must be shown no figure rather than a zero — unless a
    /// migrated opening balance gives it one, or the order is a net credit, on which no bill
    /// is ever due and the zero is a fact. With links, Xero has answered, and the answer
    /// runs from nothing settled through to the order's full value.
    /// </summary>
    public static WorkOrderPaymentStatus For(int linkedLineCount, decimal paid, decimal value)
    {
        if (linkedLineCount == 0)
        {
            if (paid != 0m) return WorkOrderPaymentStatus.OpeningBalance;
            // A net-credit order will never have a supplier bill: no money is owed on it, so
            // there is nothing for a bill to say. "Not linked" would hold its credit out of
            // the remaining figure for ever; NoBillDue says the GBP 0.00 paid is a fact.
            // Should a supplier credit note ever be entered in Xero and linked to it,
            // linkedLineCount moves off zero and the linked ladder below takes over.
            return value < 0m ? WorkOrderPaymentStatus.NoBillDue : WorkOrderPaymentStatus.NotLinked;
        }

        if (paid <= 0m) return WorkOrderPaymentStatus.Unpaid;
        return value > 0m && paid >= value ? WorkOrderPaymentStatus.Paid : WorkOrderPaymentStatus.PartPaid;
    }
}

public sealed record WorkOrderInvoiceSummary(
    string WorkOrderId,
    int Number,
    string Title,
    string SubcontractorName,
    WorkOrderStatus Status,
    decimal Value,
    decimal InvoicedToDate,
    decimal RemainingToInvoice,
    int LinkedLineCount,
    WorkOrderInvoicingStatus InvoicingStatus,
    // What the order has actually been paid, and how much that figure can be trusted.
    // PaidToDate is meaningless unless PaymentStatus says otherwise — read the status first.
    decimal PaidToDate,
    WorkOrderPaymentStatus PaymentStatus,
    // When JPMS last heard from Xero. Project-wide, so the same value repeats on every row —
    // the paid figures are only as current as this, and the sync is a button someone presses
    // rather than a schedule. Null when no purchase line has ever been synced.
    DateTimeOffset? LedgerSyncedAtUtc);
