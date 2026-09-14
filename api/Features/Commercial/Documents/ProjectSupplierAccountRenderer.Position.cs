using MigraDoc.DocumentObjectModel;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    // Where the two halves leave the account, in one breath — the paragraph the accountant would
    // otherwise write by hand: the over-invoice (or the headroom), what is not yet linked, and the
    // cash that has actually gone out against the invoices with the CIS withheld from it.
    private static void AddPosition(Section section, ProjectSupplierAccount account)
    {
        var position = account.IsOverInvoiced
            ? $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(account.OverOrders)} over the orders."
            : $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(-account.OverOrders)} still to be claimed.";
        var paragraph = Panelled(section, "");
        paragraph.AddFormattedText(position, new Font { Bold = true, Color = account.IsOverInvoiced ? Negative : Navy });
        paragraph.AddText(
            $"  Not yet linked to an order: {Money(account.Unmatched)}.  Settled by Xero: {Money(account.ReceivedAndSettled)} of the invoices received"
            + $" — payments made {Money(account.PaymentsMade)} after {Money(account.CisDeducted)} CIS deducted.");
        SpaceAfterTable(section);
    }
}
