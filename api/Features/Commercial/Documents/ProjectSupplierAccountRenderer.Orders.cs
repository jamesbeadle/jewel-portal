using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private const int OrderColumnCount = 6;
    private const int OrderDescriptionColumn = 1;
    private const int OrderValueColumn = 2;
    private const int OrderInvoicedColumn = 3;
    private const int OrderPaidColumn = 4;
    private const int OrderRemainingColumn = 5;

    // The orders' side: one row per order with its figures, its priced lines beneath at value
    // only (the invoiced and paid positions are the ORDER's — a link ties a bill to an order, never
    // to a line), then the invoices linked to it, so the reader can see where every figure came from.
    private static void AddOrders(Section section, ProjectSupplierAccount account)
    {
        SectionHeading(section, "Work orders held");
        var table = AddLinesTable(section,
            (1.8, "Order"), (6.6, "Description"), (2.3, "Order value"),
            (2.4, "Invoiced and linked"), (2.3, "Paid"), (2.4, "Left to invoice"));
        SetMoneyColumns(table, OrderValueColumn, OrderInvoicedColumn, OrderPaidColumn, OrderRemainingColumn);

        if (account.Orders.Count == 0)
            AddEmptyRow(table, OrderColumnCount, "No live work orders are held with this supplier on the project.");

        foreach (var order in account.Orders)
        {
            AddOrderRow(table, order);
            foreach (var line in order.Lines) AddOrderLineRow(table, line);
            AddLinkedInvoicesRow(table, order);
        }

        var totals = AddTotalRow(table, OrderValueColumn, "Total", account.Ordered);
        MoneyCell(totals.Cells[OrderInvoicedColumn], account.InvoicedAndLinked, bold: true);
        MoneyCell(totals.Cells[OrderPaidColumn], account.Paid, bold: true);
        MoneyCell(totals.Cells[OrderRemainingColumn], account.LeftToInvoice, bold: true, colour: NegativeWhenBelowZero(account.LeftToInvoice));
        SpaceAfterTable(section);
    }

    private static void AddOrderRow(Table table, ProjectSupplierAccountOrder order)
    {
        var row = AddPaddedRow(table);
        TextCell(row.Cells[0], order.Reference, emphasis: TextEmphasis.Reference);
        TextCell(row.Cells[OrderDescriptionColumn], OrderDescription(order), emphasis: TextEmphasis.Bold);
        MoneyCell(row.Cells[OrderValueColumn], order.Value, bold: true);
        MoneyCell(row.Cells[OrderInvoicedColumn], order.InvoicedToDate, bold: true);
        if (order.IsPaymentKnown) MoneyCell(row.Cells[OrderPaidColumn], order.PaidToDate, bold: true);
        else TextCell(row.Cells[OrderPaidColumn], "–", emphasis: TextEmphasis.Muted, alignRight: true);
        MoneyCell(row.Cells[OrderRemainingColumn], order.RemainingToInvoice, bold: true, colour: NegativeWhenBelowZero(order.RemainingToInvoice));
    }

    private static string OrderDescription(ProjectSupplierAccountOrder order)
    {
        var title = string.IsNullOrWhiteSpace(order.Title) ? "—" : order.Title;
        return $"{title}  ·  {StatusLabel(order.Status)}, awarded {Date(order.AwardedAt)}";
    }

    private static void AddOrderLineRow(Table table, ProjectSupplierAccountOrderLine line)
    {
        var row = AddPaddedRow(table);
        var description = string.IsNullOrWhiteSpace(line.CostCode) ? line.Title : $"{line.Title}  ·  {line.CostCode}";
        TextCell(row.Cells[OrderDescriptionColumn], description, emphasis: TextEmphasis.Indented);
        MoneyCell(row.Cells[OrderValueColumn], line.LineTotal, colour: Muted);
    }

    private static void AddLinkedInvoicesRow(Table table, ProjectSupplierAccountOrder order)
    {
        var row = AddPaddedRow(table);
        var text = order.Invoices.Count == 0
            ? "No invoice linked to this order yet."
            : "Invoices linked: " + string.Join("  ·  ", order.Invoices.Select(invoice => $"{invoice.InvoiceNumber} {Money(invoice.Amount)}"));
        TextCell(row.Cells[OrderDescriptionColumn], text, emphasis: TextEmphasis.IndentedNote);
    }

    private static string StatusLabel(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Released => "Released",
        WorkOrderStatus.Complete => "Complete",
        _ => status.ToString()
    };
}
