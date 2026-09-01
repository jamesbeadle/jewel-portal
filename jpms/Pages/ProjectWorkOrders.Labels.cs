using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    private static string PaymentLabel(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.NotLinked => "Not linked",
        WorkOrderPaymentStatus.Unpaid => "Unpaid",
        WorkOrderPaymentStatus.PartPaid => "Part paid",
        WorkOrderPaymentStatus.Paid => "Paid",
        WorkOrderPaymentStatus.NoBillDue => "No bill due",
        _ => "Opening balance"
    };

    private static string PaymentClass(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.Paid => "text-positive",
        WorkOrderPaymentStatus.NotLinked => "text-content-subtle italic",
        _ => "text-content-subtle"
    };

    private static string PaymentTitle(WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.NotLinked => UnknownPaidHint,
        WorkOrderPaymentStatus.Unpaid => "Bills are linked to this order and Xero has settled none of them yet",
        WorkOrderPaymentStatus.PartPaid => "Xero has settled some of the bills linked to this order",
        WorkOrderPaymentStatus.Paid => "Xero has settled the bills linked to this order to its full value",
        WorkOrderPaymentStatus.NoBillDue => "A credit order — no supplier bill is ever due on it, so its £0.00 paid is a fact and its full credit counts in the remaining figure",
        _ => "Carried over from Buildertrend at migration — no Xero bill is linked to this order yet"
    };

    // Project-wide, so any row carries it; null until a purchase line has ever been synced.
    private DateTimeOffset? LedgerSyncedAtUtc =>
        SummariesByOrder.Values.Select(summary => summary.LedgerSyncedAtUtc).FirstOrDefault();

    private static string Ago(DateTimeOffset when)
    {
        var span = DateTimeOffset.UtcNow - when;
        if (span < TimeSpan.FromMinutes(2)) return "moments ago";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes} minutes ago";
        if (span < TimeSpan.FromDays(2)) return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")} ago";
        return $"{(int)span.TotalDays} days ago";
    }

    private static string InvoicingLabel(WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.NotInvoiced => "Not invoiced",
        WorkOrderInvoicingStatus.PartInvoiced => "Part invoiced",
        WorkOrderInvoicingStatus.FullyInvoiced => "Fully invoiced",
        _ => "Over invoiced"
    };

    private static string InvoicingClass(WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.FullyInvoiced => "text-positive",
        WorkOrderInvoicingStatus.OverInvoiced => "text-negative",
        _ => "text-content-subtle"
    };

    private static string MoneyExact(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

    // InvoicedShare spreads a whole-order invoiced figure across its lines in proportion to
    // value, and decimal division leaves sub-penny residue: a fully invoiced order can come
    // out at −£0.004 left to invoice, which renders as “£0.00” in over-invoiced red. Snap a
    // figure to pennies before deciding whether it is negative, positive or zero — and before
    // showing it — so a residue smaller than money can express never raises a false alarm.
    private static decimal AtPennies(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
