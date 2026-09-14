namespace Jewel.JPMS.Features.Procurement;

/// <summary>How the supplier account's figures and position read on screen — shared by the modal
/// and its two tables, defined once. The labels live with the model (ProjectSupplierInvoiceStates)
/// and the tones in StatusTones, so the PDF and the screen say the same words.</summary>
public static class SupplierAccountDisplay
{
    public const string RowActionHint =
        "This supplier's account on the project: orders, invoices received, paid, left to invoice and any over-invoice, with a PDF to send on";

    /// <summary>Which orders an invoice pays, and how much of it is linked to none of them.</summary>
    public static string MatchedText(ProjectSupplierAccountInvoice invoice)
    {
        if (!invoice.IsMatched) return "Not linked to an order";
        var orders = string.Join(", ", invoice.Orders.Select(order => order.Reference));
        if (invoice.Unmatched == 0m) return orders;
        return $"{orders} ({Money(invoice.Matched)}) · {Money(invoice.Unmatched)} not linked";
    }

    /// <summary>The one sentence the account adds up to — the same reading the PDF prints.</summary>
    public static string PositionText(ProjectSupplierAccount account) =>
        account.IsOverInvoiced
            ? $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(account.OverOrders)} over the orders."
            : $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(-account.OverOrders)} still to be claimed.";

    public static string UnmatchedText(ProjectSupplierAccount account)
    {
        var awaiting = account.Invoices.Count(invoice => invoice.State == ProjectSupplierInvoiceState.AwaitingApproval);
        var awaitingNote = awaiting == 0 ? "" : $", of which {awaiting} invoice{(awaiting == 1 ? "" : "s")} awaiting approval in Xero";
        return $"Not yet linked to an order: {Money(account.Unmatched)}{awaitingNote}. Settled by Xero: {Money(account.ReceivedAndSettled)} of the invoices received.";
    }

    public static string PdfPath(string projectId, string subcontractorId) =>
        $"/api/projects/{projectId}/suppliers/{subcontractorId}/account/pdf";
}
