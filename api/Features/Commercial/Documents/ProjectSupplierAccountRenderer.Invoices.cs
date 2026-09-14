using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private const int InvoiceColumnCount = 7;
    private const int InvoiceDateColumn = 1;
    private const int InvoiceLabourColumn = 2;
    private const int InvoiceMaterialsColumn = 3;
    private const int InvoiceNetColumn = 4;
    private const int InvoiceStateColumn = 5;
    private const int InvoiceMatchedColumn = 6;

    // The invoices' side: every bill received for the project, linked or not, with the supplier's
    // own CIS split, where Xero says it stands, and the order(s) it pays. A bill nobody has placed
    // yet says so beneath its row rather than hiding — it is exactly the one an over-invoice hides in.
    private static void AddInvoices(Section section, ProjectSupplierAccount account)
    {
        SectionHeading(section, "Invoices received");
        var table = AddLinesTable(section,
            (2.4, "Invoice"), (2.0, "Date"), (2.3, "Labour"), (2.3, "Materials"),
            (2.3, "Net"), (3.1, "Status"), (3.4, "Matched to"));
        SetMoneyColumns(table, InvoiceLabourColumn, InvoiceMaterialsColumn, InvoiceNetColumn);

        if (account.Invoices.Count == 0)
            AddEmptyRow(table, InvoiceColumnCount, "No invoices have been received from this supplier for the project.");

        foreach (var invoice in account.Invoices)
        {
            AddInvoiceRow(table, invoice);
            foreach (var note in NotesFor(invoice)) AddNoteRow(table, note);
        }

        var totals = AddTotalRow(table, InvoiceLabourColumn, "Received", account.LabourReceived);
        MoneyCell(totals.Cells[InvoiceMaterialsColumn], account.MaterialsReceived, bold: true);
        MoneyCell(totals.Cells[InvoiceNetColumn], account.Received, bold: true);
        SpaceAfterTable(section);
    }

    private static void AddInvoiceRow(Table table, ProjectSupplierAccountInvoice invoice)
    {
        var row = AddPaddedRow(table);
        var number = invoice.IsCreditNote ? $"{invoice.InvoiceNumber} (credit note)" : invoice.InvoiceNumber;
        TextCell(row.Cells[0], number, emphasis: TextEmphasis.Reference);
        TextCell(row.Cells[InvoiceDateColumn], invoice.Date is { } date ? Date(date) : "—", emphasis: TextEmphasis.Muted);
        MoneyCell(row.Cells[InvoiceLabourColumn], invoice.Labour);
        MoneyCell(row.Cells[InvoiceMaterialsColumn], invoice.Materials);
        MoneyCell(row.Cells[InvoiceNetColumn], invoice.Net, bold: true);
        TextCell(row.Cells[InvoiceStateColumn], invoice.State.Label(), emphasis: TextEmphasis.Muted);
        TextCell(row.Cells[InvoiceMatchedColumn], MatchedLabel(invoice));
    }

    private static string MatchedLabel(ProjectSupplierAccountInvoice invoice)
    {
        if (!invoice.IsMatched) return "Not linked to an order";
        var orders = string.Join(", ", invoice.Orders.Select(order => order.Reference));
        return invoice.Unmatched == 0m ? orders : $"{orders} ({Money(invoice.Matched)}); {Money(invoice.Unmatched)} not linked";
    }

    private static IEnumerable<string> NotesFor(ProjectSupplierAccountInvoice invoice)
    {
        var placement = invoice.Placement.Label();
        if (placement.Length > 0) yield return placement;
        if (invoice.NetElsewhere != 0m) yield return $"{Money(invoice.NetElsewhere)} of this bill sits outside the project (another project, ignored or bucketed).";
    }

    private static void AddNoteRow(Table table, string note)
    {
        var row = AddPaddedRow(table);
        row.Cells[0].MergeRight = InvoiceColumnCount - 1;
        TextCell(row.Cells[0], note, emphasis: TextEmphasis.IndentedNote);
    }

    // Where the two halves leave the account, in one breath — the paragraph the accountant would
    // otherwise write by hand.
    private static void AddPosition(Section section, ProjectSupplierAccount account)
    {
        var position = account.IsOverInvoiced
            ? $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(account.OverOrders)} over the orders."
            : $"Invoices received total {Money(account.Received)} against orders totalling {Money(account.Ordered)} — {Money(-account.OverOrders)} still to be claimed.";
        var paragraph = Panelled(section, "");
        paragraph.AddFormattedText(position, new Font { Bold = true, Color = account.IsOverInvoiced ? Negative : Navy });
        paragraph.AddText(
            $"  Not yet linked to an order: {Money(account.Unmatched)}.  Settled by Xero: {Money(account.ReceivedAndSettled)} of the invoices received.");
        SpaceAfterTable(section);
    }
}
