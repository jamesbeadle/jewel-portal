using System.Globalization;

namespace Jewel.JPMS.Features.Procurement;

/// <summary>How the work-orders tab's figures, statuses and links read — shared by the page,
/// its lists and its table rows, defined once.</summary>
public static class WorkOrderDisplay
{
    /// <summary>The printable purchase order — issued orders and drafts alike (a draft renders
    /// with a Draft reference and badge, so it can be read before approving releases it).</summary>
    public static string PurchaseOrderPath(string projectId, string workOrderId) =>
        $"/projects/{projectId}/work-orders/{workOrderId}/po";

    /// <summary>An em dash, not a zero: "£0.00 paid" and "we have not been told what is paid"
    /// are different answers, and the second must not be able to pass for the first.</summary>
    public const string Dash = "–";

    public const string UnknownPaidHint =
        "No Xero bill is linked to this order yet, so nothing is known about what has been paid — link its bills on the WO Allocation tab";

    public const string UnknownCommittedHint =
        "Committed value on orders with no Xero bill linked — nothing is known about what has " +
        "been paid on them, so they count in neither paid nor remaining. Link their bills on " +
        "the WO Allocation tab and they move into the remaining figure.";

    public static string PaymentLabel(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.NotLinked => "Not linked",
        WorkOrderPaymentStatus.Unpaid => "Unpaid",
        WorkOrderPaymentStatus.PartPaid => "Part paid",
        WorkOrderPaymentStatus.Paid => "Paid",
        WorkOrderPaymentStatus.NoBillDue => "No bill due",
        _ => "Opening balance"
    };

    public static string PaymentClass(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.Paid => "text-positive",
        WorkOrderPaymentStatus.NotLinked => "text-content-subtle italic",
        _ => "text-content-subtle"
    };

    public static string PaymentTitle(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.NotLinked => UnknownPaidHint,
        WorkOrderPaymentStatus.Unpaid => "Bills are linked to this order and Xero has settled none of them yet",
        WorkOrderPaymentStatus.PartPaid => "Xero has settled some of the bills linked to this order",
        WorkOrderPaymentStatus.Paid => "Xero has settled the bills linked to this order to its full value",
        WorkOrderPaymentStatus.NoBillDue => "A credit order — no supplier bill is ever due on it, so its £0.00 paid is a fact and its full credit counts in the remaining figure",
        _ => "Carried over from Buildertrend at migration — no Xero bill is linked to this order yet"
    };

    public static string Ago(DateTimeOffset when)
    {
        var span = DateTimeOffset.UtcNow - when;
        if (span < TimeSpan.FromMinutes(2)) return "moments ago";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes} minutes ago";
        if (span < TimeSpan.FromDays(2)) return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")} ago";
        return $"{(int)span.TotalDays} days ago";
    }

    public static string InvoicingLabel(WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.NotInvoiced => "Not invoiced",
        WorkOrderInvoicingStatus.PartInvoiced => "Part invoiced",
        WorkOrderInvoicingStatus.FullyInvoiced => "Fully invoiced",
        _ => "Over invoiced"
    };

    public static string InvoicingClass(WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.FullyInvoiced => "text-positive",
        WorkOrderInvoicingStatus.OverInvoiced => "text-negative",
        _ => "text-content-subtle"
    };

    public static string MoneyExact(decimal value) =>
        value.ToString("C2", CultureInfo.GetCultureInfo("en-GB"));

    /// <summary>Snaps a figure to pennies before deciding whether it is negative, positive or
    /// zero — and before showing it. InvoicedShare spreads a whole-order invoiced figure across
    /// its lines in proportion to value, and decimal division leaves sub-penny residue: a fully
    /// invoiced order can come out at −£0.004 left to invoice, which would render as "£0.00" in
    /// over-invoiced red; a residue smaller than money can express never raises a false alarm.</summary>
    public static decimal AtPennies(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
