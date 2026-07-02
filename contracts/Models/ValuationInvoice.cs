namespace Jewel.JPMS.Models;

// Lifecycle of a valuation invoice (formerly "cash call"): Raised (drafted against the current
// valuation), Issued (client invoice sent — counts toward certified/invoiced to date), Paid (client
// has paid — rolls into the project-level paid total).
public enum ValuationInvoiceStatus
{
    Raised = 0,
    Issued = 1,
    Paid = 2
}

// A valuation invoice: the client invoice raised against the current valuation/CVR. Drawn from a
// valuation claim when one is linked. Issued invoices drive "Certified to date" on the valuation
// report; when paid, the amount rolls into the project-level total.
public sealed record ValuationInvoice(
    string ValuationInvoiceId,
    string ProjectId,
    string? ValuationClaimId,
    int Number,
    string Reference,             // e.g. "VI-0001"
    DateTimeOffset PeriodMonth,   // the month this invoice covers
    decimal Amount,
    decimal AmountPaid,
    ValuationInvoiceStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? PaidAt = null)
{
    public string DisplayNumber => Number > 0 ? $"VI-{Number:0000}" : "";
}

// Project-level roll-up of valuation invoices.
public sealed record ProjectValuationInvoiceSummary(
    string ProjectId,
    decimal TotalRaised,      // sum of all invoice amounts, any status
    decimal TotalInvoiced,    // sum of Issued + Paid invoice amounts — feeds "Certified to date"
    decimal TotalPaid,        // sum of amounts the client has paid
    decimal Outstanding);     // invoiced but not yet paid
