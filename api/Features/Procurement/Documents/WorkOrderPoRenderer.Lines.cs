using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    private static void AddSummaryTable(Section section, WorkOrderPoDocumentModel model)
    {
        var table = AddLinedTable(section,
            (9.8, "PO Title", false), (4.0, "Scheduled Completion", false), (4.0, "Total Price", true));

        var row = AddPaddedRow(table, 1.4);
        var title = row.Cells[0].AddParagraph(TitleOrSupplier(model));
        title.Format.LeftIndent = Unit.FromMillimeter(1.5);
        title.Format.Font.Size = 9;
        title.Format.Font.Bold = true;
        var when = row.Cells[1].AddParagraph(CompletionOrDash(model));
        when.Format.LeftIndent = Unit.FromMillimeter(1.5);
        when.Format.Font.Size = 9;
        MoneyCell(row.Cells[2], model.Order.Value, bold: true);
        SpaceAfterTable(section);
    }

    private static void AddLinesTable(Section section, WorkOrderPoDocumentModel model)
    {
        if (model.Lines.Count == 0) return;

        SectionHeading(section, "Order Lines");
        var table = AddLinedTable(section,
            (3.4, "Items", false), (2.0, "Cost Types", false), (4.6, "Description", false),
            (1.9, "Qty/Unit", true), (2.0, "Unit Cost", true), (2.0, "Price", true), (1.9, "Paid", true));

        foreach (var line in model.Lines.OrderBy(line => line.SortOrder))
            AddLineRow(table, line);

        var totalPaid = model.Lines.Sum(line => line.PaidToDate);
        AddTotalsRow(table, model.Lines.Sum(line => line.LineTotal), totalPaid);
        AddRemainingBalance(section, model.Order.Value - totalPaid);
    }

    private static void AddLineRow(Table table, WorkOrderLine line)
    {
        var row = AddPaddedRow(table, 1.2);

        var item = row.Cells[0].AddParagraph();
        item.Format.LeftIndent = Unit.FromMillimeter(1.5);
        item.Format.Font.Size = 8.5;
        item.AddFormattedText(line.Title, new Font { Bold = true });
        if (!string.IsNullOrWhiteSpace(line.CostCode))
            item.AddFormattedText($"  · {line.CostCode}", new Font { Color = Muted, Size = 7.5 });

        var type = row.Cells[1].AddParagraph(string.IsNullOrWhiteSpace(line.CostType) ? "—" : line.CostType);
        type.Format.LeftIndent = Unit.FromMillimeter(1.5);
        type.Format.Font.Size = 8;
        type.Format.Font.Color = Muted;

        // Pre-wrap: a multi-line description keeps its typed line breaks, same as the sheet.
        PrewrapCell(row.Cells[2], line.Description);

        var quantity = row.Cells[3].AddParagraph($"{line.Quantity.ToString("0.##", Uk)} {line.Unit}".Trim());
        quantity.Format.RightIndent = Unit.FromMillimeter(1.5);
        quantity.Format.Font.Size = 8.5;

        MoneyCell(row.Cells[4], line.UnitCost);
        MoneyCell(row.Cells[5], line.LineTotal);
        PaidCell(row.Cells[6], line.PaidToDate);
    }

    // Nothing paid prints as a dash rather than a zero, so the column reads at a glance.
    private static void PaidCell(Cell cell, decimal paid)
    {
        if (paid != 0m) { MoneyCell(cell, paid); return; }
        var dash = cell.AddParagraph("–");
        dash.Format.RightIndent = Unit.FromMillimeter(1.5);
        dash.Format.Font.Size = 8.5;
        dash.Format.Font.Color = Muted;
    }

    private static void AddTotalsRow(Table table, decimal total, decimal totalPaid)
    {
        var totals = AddPaddedRow(table, 1.4);
        totals.Shading.Color = Panel;
        var label = totals.Cells[2].AddParagraph("Totals");
        label.Format.LeftIndent = Unit.FromMillimeter(1.5);
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[5], total, bold: true);
        MoneyCell(totals.Cells[6], totalPaid, bold: true);
    }

    private static void AddRemainingBalance(Section section, decimal remainingBalance)
    {
        var remaining = section.AddParagraph();
        remaining.Format.Alignment = ParagraphAlignment.Right;
        remaining.Format.Font.Size = 9;
        SpaceBefore(remaining, 1.5);
        remaining.AddFormattedText("Remaining Balance:  ", new Font { Bold = true, Color = Navy });
        remaining.AddFormattedText(Money(remainingBalance), new Font { Bold = true });
        SpaceAfter(remaining, 2);
    }
}
