namespace Jewel.JPMS.Models;

/// <summary>
/// One supplier's account on one project — see Contracts.Commercial.GetProjectSupplierAccount.
/// Two halves that reconcile: the orders the supplier holds here (what we agreed to pay) and the
/// invoices received from them for the project (what they have asked for), whether or not anyone
/// has linked an invoice to an order yet. All money is net of VAT and signed — credit notes carry
/// negative figures — so every total is a plain sum.
/// </summary>
public sealed record ProjectSupplierAccount(
    string ProjectId,
    string ProjectReference,
    string ProjectName,
    string SubcontractorId,
    string SupplierName,
    DateTimeOffset GeneratedAt,
    // When JPMS last heard from Xero. Paid figures and invoice statuses are only as current as
    // this; null when no purchase line has ever been synced.
    DateTimeOffset? LedgerSyncedAtUtc,
    IReadOnlyList<ProjectSupplierAccountOrder> Orders,
    IReadOnlyList<ProjectSupplierAccountInvoice> Invoices)
{
    // ── The orders' side: what was agreed. ──
    public decimal Ordered => Orders.Sum(order => order.Value);
    public decimal InvoicedAndLinked => Orders.Sum(order => order.InvoicedToDate);
    public decimal Paid => Orders.Sum(order => order.PaidToDate);
    public decimal LeftToInvoice => Ordered - InvoicedAndLinked;

    // ── The invoices' side: what was asked for. ──
    public decimal LabourReceived => Invoices.Sum(invoice => invoice.Labour);
    public decimal MaterialsReceived => Invoices.Sum(invoice => invoice.Materials);
    public decimal Received => Invoices.Sum(invoice => invoice.Net);
    public decimal ReceivedAndSettled => Invoices.Sum(invoice => invoice.SettledNet);

    /// <summary>Received but not linked to any of the supplier's orders on this project — the
    /// part of the account nobody has matched yet, including bills still awaiting approval.</summary>
    public decimal Unmatched => Invoices.Sum(invoice => invoice.Unmatched);

    /// <summary>What the supplier has invoiced beyond the orders they hold here. Positive is an
    /// over-invoice to query before it is paid; zero or below is headroom still to be claimed.</summary>
    public decimal OverOrders => Received - Ordered;

    public bool IsOverInvoiced => OverOrders > 0m;

    public bool HasInvoicesAwaitingApproval =>
        Invoices.Any(invoice => invoice.State == ProjectSupplierInvoiceState.AwaitingApproval);
}
