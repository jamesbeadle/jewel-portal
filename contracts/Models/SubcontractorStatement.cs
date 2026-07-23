namespace Jewel.JPMS.Models;

// A subcontractor's statement of account: every work order they hold, grouped by project, with the
// Xero purchase invoices claimed against each order — the same link data behind the WO Allocation
// tab, presented from the supplier's side of the ledger. Figures are net (pre-VAT); credit notes
// carry negative amounts, so invoiced-to-date is always the signed sum of what was claimed.
public sealed record SubcontractorStatement(
    string SubcontractorId,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<SubcontractorStatementProject> Projects)
{
    public int OrderCount => Projects.Sum(project => project.Orders.Count);
    public decimal TotalOrdered => Projects.Sum(project => project.Ordered);
    public decimal TotalInvoiced => Projects.Sum(project => project.Invoiced);
    public decimal TotalRemaining => TotalOrdered - TotalInvoiced;
}

// One project's slice of the statement: the orders the subcontractor holds there.
public sealed record SubcontractorStatementProject(
    string ProjectId,
    string ProjectReference,
    string ProjectName,
    IReadOnlyList<SubcontractorStatementOrder> Orders)
{
    public decimal Ordered => Orders.Sum(order => order.Value);
    public decimal Invoiced => Orders.Sum(order => order.InvoicedToDate);
    public decimal Remaining => Ordered - Invoiced;
}

// One work order on the statement with the invoices claimed against it. InvoicedToDate is the
// signed sum of the invoice amounts below, so the order's remaining balance always reconciles
// with the invoice list the statement prints.
public sealed record SubcontractorStatementOrder(
    string WorkOrderId,
    int Number,
    string Title,
    WorkOrderStatus Status,
    DateTimeOffset AwardedAt,
    decimal Value,
    decimal InvoicedToDate,
    IReadOnlyList<SubcontractorStatementInvoice> Invoices)
{
    public string Reference => $"WO-{Number:0000}";
    public decimal RemainingToInvoice => Value - InvoicedToDate;
}

// One purchase invoice (or credit note) claimed against an order: the Xero invoice's identity and
// the signed share of its value linked to that order. A bill split across several orders appears
// on each order it pays, carrying only that order's share.
public sealed record SubcontractorStatementInvoice(
    string XeroInvoiceId,
    string InvoiceNumber,
    string? InvoiceReference,
    DateTime? Date,
    bool IsCreditNote,
    decimal Amount);

/// <summary>
/// The outcome of drafting a statement-of-account email in the shared mailbox: who the draft is
/// addressed to (the subcontractor's directory email) and where to open it. <see cref="WebLink"/>
/// opens the draft in Outlook on the web when Graph returns one (it usually does); null otherwise —
/// the draft is still in the mailbox's Drafts folder. Mirrors <see cref="WorkOrderEmailDraft"/>.
/// </summary>
public sealed record SubcontractorStatementEmailDraft(
    string SubcontractorId,
    string CompanyName,
    string Subject,
    string RecipientEmail,
    string? WebLink);
