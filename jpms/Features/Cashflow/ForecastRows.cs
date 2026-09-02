using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Features.Cashflow;

/// <summary>One category row of the forecast table: which phased category it shows, how it is
/// labelled, and the one-line provenance beneath the label.</summary>
public sealed record ForecastRowInfo(ForecastCategory Category, string Label, string Source);

/// <summary>The forecast table's two bands, in the order they read — cash in, then cash out.</summary>
public static class ForecastRows
{
    public static readonly ForecastRowInfo[] In =
    {
        new(ForecastCategory.InvoicesOutstanding, "Valuation invoices outstanding",
            "already issued (or awaiting approval) · lands a payment-mechanism lag after issue"),
        new(ForecastCategory.FutureValuations, "Future valuations",
            "left to claim — spread evenly to practical completion, or claimed at the project's expected £/month where set — each valuation paid its contract's payment terms after the valuation date"),
        new(ForecastCategory.RetentionReleases, "Retention releases",
            "R1 at practical completion · R2 after the defects period")
    };

    public static readonly ForecastRowInfo[] Out =
    {
        new(ForecastCategory.BillsUnpaid, "Supplier bills unpaid",
            "part-payment aware · assumed payable this month (per-bill due dates pending)"),
        new(ForecastCategory.WorkOrdersToInvoice, "Work orders still to invoice",
            "committed less invoiced, spread to practical completion, paid a month later"),
        new(ForecastCategory.DrawdownsToSpend, "Drawdowns still to spend",
            "budget beyond orders and bills, spread to practical completion, paid a month later")
    };
}
