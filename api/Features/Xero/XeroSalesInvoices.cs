namespace Jewel.JPMS.Api.Features.Xero;

// The sales side read BACK from Xero (2026-09-11, the MD's ask: "the portal says it can't
// recognise when a sales invoice is paid"). Until now the portal's only sales-side read was the
// aged receivables — AUTHORISED / DRAFT / SUBMITTED with money due — so a PAID invoice was
// invisible to it. Xero is the home of what has been paid; the portal READS it rather than being
// told, and these are the shapes it reads: every ACCREC invoice a contact has, whatever its status.

/// <summary>
/// One sales invoice as Xero holds it right now, in every status but DELETED. AmountPaid /
/// AmountDue are Xero's own figures (gross, VAT included); SubTotal is the net the portal's
/// valuation invoice Amount is compared with. FullyPaidOnDate is set once Xero has the invoice
/// PAID — the date the portal stamps as PaidAt.
/// </summary>
public sealed record XeroSalesInvoiceSummary(
    string InvoiceId,
    string? Number,
    string? Reference,
    string Status,
    DateTime? Date,
    DateTime? DueDate,
    decimal SubTotal,
    decimal TotalTax,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue,
    DateTime? FullyPaidOnDate,
    string? CurrencyCode)
{
    /// <summary>Settled in Xero: nothing left due and something paid — the same reading whether
    /// Xero says PAID outright or an AUTHORISED invoice was fully allocated a moment ago.</summary>
    public bool IsPaid => AmountDue == 0m && AmountPaid > 0m;

    /// <summary>Something paid, something still due.</summary>
    public bool IsPartPaid => AmountPaid > 0m && AmountDue > 0m;

    public bool IsVoided => Status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Every sales invoice Xero holds for one contact, read fresh (no cache — a payment is what the
/// reader is looking for and it can have landed a minute ago). IsConfigured false / Error set
/// mean nothing was read; Invoices is then empty.
/// </summary>
public sealed record XeroSalesInvoiceListSnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<XeroSalesInvoiceSummary> Invoices)
{
    public static XeroSalesInvoiceListSnapshot NotConfigured() => new(false, null, null, Array.Empty<XeroSalesInvoiceSummary>());
    public static XeroSalesInvoiceListSnapshot Failed(string error) => new(true, error, null, Array.Empty<XeroSalesInvoiceSummary>());
}
