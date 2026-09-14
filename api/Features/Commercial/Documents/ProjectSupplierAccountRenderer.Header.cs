using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private static void AddHeaderBand(Section section, ProjectSupplierAccount account)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;

        var row = table.AddRow();
        row.Shading.Color = Navy;
        row.TopPadding = Unit.FromMillimeter(4);
        row.BottomPadding = Unit.FromMillimeter(4);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(4);
        row.Cells[1].Format.RightIndent = Unit.FromMillimeter(4);
        row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

        AddHeaderTitle(row.Cells[0], account);
        AddHeaderStamp(row.Cells[1], account);
        Hairline(section);
    }

    private static void AddHeaderTitle(Cell cell, ProjectSupplierAccount account)
    {
        DocumentBranding.AddLogo(cell, Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = cell.AddParagraph("SUPPLIER ACCOUNT");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var subtitle = cell.AddParagraph(ProjectLabel(account));
        subtitle.Format.Font.Size = 9.5;
        subtitle.Format.Font.Bold = true;
        subtitle.Format.Font.Color = Gold;
    }

    private static void AddHeaderStamp(Cell cell, ProjectSupplierAccount account)
    {
        var stamp = cell.AddParagraph(account.SupplierName.ToUpperInvariant());
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = cell.AddParagraph($"Account at  {Date(account.GeneratedAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;
    }

    private static void AddDetailsGrid(Section section, ProjectSupplierAccount account)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(1.5);
        spacer.Format.Font.Size = 2;

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelWidth = Unit.FromCentimeter(3.3);
        var valueWidth = Unit.FromCentimeter(5.6);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);

        AddGridRow(table, "Supplier", account.SupplierName, "Project", ProjectLabel(account));
        AddGridRow(table, "Work orders", account.Orders.Count.ToString(Uk), "Xero synced", SyncedLabel(account));
        AddGridRow(table, "Ordered", Money(account.Ordered), "Invoices received", Money(account.Received));
        AddGridRow(table, "Invoiced and linked", Money(account.InvoicedAndLinked), "Left to invoice", Money(account.LeftToInvoice));
        AddGridRow(table, "Paid", Money(account.Paid), "Over the orders", OverOrdersLabel(account));

        SpaceAfterTable(section);
    }

    private static string ProjectLabel(ProjectSupplierAccount account) =>
        string.IsNullOrWhiteSpace(account.ProjectReference)
            ? account.ProjectName
            : $"{account.ProjectReference} — {account.ProjectName}";

    private static string SyncedLabel(ProjectSupplierAccount account) =>
        account.LedgerSyncedAtUtc is { } syncedAt ? DateAndTime(syncedAt) : "never";

    private static string OverOrdersLabel(ProjectSupplierAccount account) =>
        account.IsOverInvoiced
            ? $"{Money(account.OverOrders)} over"
            : $"None — {Money(-account.OverOrders)} still to be claimed";
}
