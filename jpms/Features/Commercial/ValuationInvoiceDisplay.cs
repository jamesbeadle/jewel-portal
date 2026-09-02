namespace Jewel.JPMS.Features.Commercial;

/// <summary>How a valuation invoice's status and history read on screen — the badge, its
/// hover text and the audit trail's event names.</summary>
public static class ValuationInvoiceDisplay
{
    public static string StatusLabel(ValuationInvoiceStatus status) => status switch
    {
        ValuationInvoiceStatus.Raised => "Draft",
        ValuationInvoiceStatus.Submitted => "Awaiting approval",
        ValuationInvoiceStatus.Approved => "Approved",
        ValuationInvoiceStatus.Rejected => "Rejected",
        ValuationInvoiceStatus.Issued => "Issued",
        ValuationInvoiceStatus.Paid => "Paid",
        ValuationInvoiceStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string StatusTitle(ValuationInvoice invoice) => invoice.Status switch
    {
        ValuationInvoiceStatus.Raised => "Draft — send the claim for approval, or issue directly",
        ValuationInvoiceStatus.Submitted => $"With the client for approval{(invoice.SubmittedAt is { } s ? $" since {s:dd MMM yyyy}" : "")}",
        ValuationInvoiceStatus.Approved => "Approved by the client — issue to count toward certified to date",
        ValuationInvoiceStatus.Rejected => "Rejected — amend and resubmit, or cancel",
        ValuationInvoiceStatus.Issued => "Issued to the client — counts toward certified to date",
        ValuationInvoiceStatus.Paid => "Paid — rolled into the project's paid total",
        ValuationInvoiceStatus.Cancelled => "Withdrawn — excluded from every total",
        _ => ""
    };

    public static string BadgeClass(ValuationInvoiceStatus status)
    {
        const string baseClass = "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium ";
        return status switch
        {
            ValuationInvoiceStatus.Paid => baseClass + "bg-accent/10 border-accent/30 text-accent",
            ValuationInvoiceStatus.Issued => baseClass + "bg-info/10 border-info/30 text-info",
            ValuationInvoiceStatus.Submitted => baseClass + "bg-warning/10 border-warning/30 text-warning",
            ValuationInvoiceStatus.Approved => baseClass + "bg-positive/10 border-positive/30 text-positive",
            ValuationInvoiceStatus.Rejected => baseClass + "bg-negative/10 border-negative/30 text-negative",
            ValuationInvoiceStatus.Cancelled => baseClass + "bg-surface-raised border-line text-content-subtle line-through",
            _ => baseClass + "bg-surface-raised border-line text-content-muted"
        };
    }

    public static string EventLabel(ValuationInvoiceEventType type) => type switch
    {
        ValuationInvoiceEventType.Created => "Created",
        ValuationInvoiceEventType.Submitted => "Submitted for approval",
        ValuationInvoiceEventType.Approved => "Approved",
        ValuationInvoiceEventType.Rejected => "Rejected",
        ValuationInvoiceEventType.Amended => "Amended",
        ValuationInvoiceEventType.Issued => "Issued",
        ValuationInvoiceEventType.PaymentRecorded => "Payment recorded",
        ValuationInvoiceEventType.Cancelled => "Cancelled",
        ValuationInvoiceEventType.ManualEntry => "Historic entry",
        _ => type.ToString()
    };
}
