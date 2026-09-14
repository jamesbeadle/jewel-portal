using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private const int InvoiceColumnCount = 8;
    private const int InvoiceDateColumn = 1;
    private const int InvoiceLabourColumn = 2;
    private const int InvoiceMaterialsColumn = 3;
    private const int InvoiceNetColumn = 4;
    private const int InvoiceCisColumn = 5;
    private const int InvoicePaymentColumn = 6;
    private const int InvoiceMatchedColumn = 7;

    // The invoices' side: every bill received for the project, linked or not, with the supplier's
    // own CIS split, the CIS Xero withheld and the cash it actually paid, where the bill stands
    // (under its number), and the order(s) it pays. A bill nobody has placed yet says so beneath
    // its row rather than hiding — it is exactly the one an over-invoice hides in.
    private static void AddInvoices(Section section, ProjectSupplierAccount account)
    {
        SectionHeading(section, "Invoices received");
        var table = AddLinesTable(section,
            (2.2, "Invoice"), (1.7, "Date"), (1.9, "Labour"), (1.9, "Materials"), (1.9, "Net"),
            (1.9, "CIS deducted"), (2.1, "Payment made"), (4.2, "Matched to"));
        SetMoneyColumns(table, InvoiceLabourColumn, InvoiceMaterialsColumn, InvoiceNetColumn, InvoiceCisColumn, InvoicePaymentColumn);

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
        MoneyCell(totals.Cells[InvoiceCisColumn], account.CisDeducted, bold: true);
        MoneyCell(totals.Cells[InvoicePaymentColumn], account.PaymentsMade, bold: true);
        SpaceAfterTable(section);
    }

    private static void AddInvoiceRow(Table table, ProjectSupplierAccountInvoice invoice)
    {
        var row = AddPaddedRow(table);
        var number = invoice.IsCreditNote ? $"{invoice.InvoiceNumber} (credit note)" : invoice.InvoiceNumber;
        TextCell(row.Cells[0], number, emphasis: TextEmphasis.Reference);
        AddSubLine(row.Cells[0], invoice.State.Label());
        TextCell(row.Cells[InvoiceDateColumn], invoice.Date is { } date ? Date(date) : "—", emphasis: TextEmphasis.Muted);
        MoneyCell(row.Cells[InvoiceLabourColumn], invoice.Labour);
        MoneyCell(row.Cells[InvoiceMaterialsColumn], invoice.Materials);
        MoneyCell(row.Cells[InvoiceNetColumn], invoice.Net, bold: true);
        MoneyOrDashCell(row.Cells[InvoiceCisColumn], invoice.CisDeduction);
        MoneyOrDashCell(row.Cells[InvoicePaymentColumn], invoice.AmountPaid);
        if (invoice.PaidOn is { } paidOn && invoice.AmountPaid != 0m) AddSubLine(row.Cells[InvoicePaymentColumn], $"on {Date(paidOn)}");
        TextCell(row.Cells[InvoiceMatchedColumn], MatchedLabel(invoice));
    }

    // Xero's zero is not a fact here: nothing paid, no deduction calculated yet (Xero works it
    // out at approval), or a line synced before the ledger carried the detail — a dash, never £0.00.
    private static void MoneyOrDashCell(Cell cell, decimal amount)
    {
        if (amount == 0m) TextCell(cell, "–", emphasis: TextEmphasis.Muted, alignRight: true);
        else MoneyCell(cell, amount);
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
        if (invoice.NetElsewhere != 0m) yield return $"{Money(invoice.NetElsewhere)} of this bill sits outside the project (another project, ignored or bucketed) — the payment and CIS figures are the whole bill's.";
    }

    private static void AddNoteRow(Table table, string note)
    {
        var row = AddPaddedRow(table);
        row.Cells[0].MergeRight = InvoiceColumnCount - 1;
        TextCell(row.Cells[0], note, emphasis: TextEmphasis.IndentedNote);
    }
}
