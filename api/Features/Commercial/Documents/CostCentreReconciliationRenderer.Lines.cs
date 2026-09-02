using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class CostCentreReconciliationRenderer
{
    private static void AddSalesLines(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Sales — valuation lines under this centre");
        var table = AddLinesTable(section, (2.0, "REF"), (12.6, "DESCRIPTION"), (3.2, "AMOUNT"));

        foreach (var line in document.SalesLines)
            AddLineRow(table, line.Reference, line.Description, line.Amount);
        if (document.SalesOther != 0m)
            AddLineRow(table, "", "Other / unlisted adjustments", document.SalesOther, muted: true);
        if (document.SalesLines.Count == 0 && document.SalesOther == 0m)
            AddEmptyRow(table, 3, "No valuation lines are coded to this centre.");

        AddTotalRow(table, 2, "Sales value", document.SalesValue);
        SpaceAfterTable(section);
    }

    private static void AddWorkOrders(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Costs — work orders");
        var table = AddLinesTable(section, (2.0, "REF"), (4.6, "SUPPLIER"), (6.2, "ORDER"), (1.8, "STATUS"), (3.2, "THIS CENTRE"));

        foreach (var order in document.WorkOrders)
            AddWorkOrderRow(table, order);
        if (document.WorkOrders.Count == 0)
            AddEmptyRow(table, 5, "No work orders are coded to this centre.");

        AddTotalRow(table, 4, "Work orders committed (drafts included)", document.WoCommitted);
        SpaceAfterTable(section);
    }

    private static void AddWorkOrderRow(Table table, ReconciliationWorkOrderLine order)
    {
        var row = AddPaddedRow(table);
        TextCell(row.Cells[0], order.Reference, mutedMono: true);
        TextCell(row.Cells[1], order.Supplier);
        TextCell(row.Cells[2], order.Title);
        TextCell(row.Cells[3], order.Status, mutedMono: true);
        MoneyCell(row.Cells[4], order.Amount);
    }

    private static void AddXeroCosts(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Costs — Xero spend not on work orders");
        var table = AddLinesTable(section, (2.2, "DATE"), (4.6, "SUPPLIER"), (2.4, "INVOICE"), (5.4, "DESCRIPTION"), (3.2, "AMOUNT"));

        foreach (var line in document.XeroCosts)
            AddCostRow(table, line.Date, line.Supplier, line.InvoiceNumber, line.Description, line.Amount);
        if (document.LabourCost != 0m)
            AddCostRow(table, "", "Internal labour", "", "Approved timesheets coded to this centre", document.LabourCost);
        if (document.OtherAdjustments != 0m)
            AddCostRow(table, "", "Other allocations & adjustments", "", "Splits and re-attributions not itemised above", document.OtherAdjustments);
        if (document.XeroCosts.Count == 0 && document.LabourCost == 0m && document.OtherAdjustments == 0m)
            AddEmptyRow(table, 5, "No non-work-order spend is allocated to this centre.");

        AddTotalRow(table, 4, "Xero & other costs", document.NonWoCost);
        SpaceAfterTable(section);
    }

    private static void AddCostRow(Table table, string date, string supplier, string invoice, string description, decimal amount)
    {
        var row = AddPaddedRow(table);
        TextCell(row.Cells[0], date, mutedMono: true);
        TextCell(row.Cells[1], supplier);
        TextCell(row.Cells[2], invoice, mutedMono: true);
        TextCell(row.Cells[3], description);
        MoneyCell(row.Cells[4], amount);
    }
}
