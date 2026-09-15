using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private static void AddHeaderBand(Section section, ProjectSupplierAccount account) =>
        CleanHeader(section, "Supplier account", ProjectLabel(account),
            new HeaderFact(account.SupplierName.ToUpperInvariant()),
            new HeaderFact($"Account at  {Date(account.GeneratedAt)}"));

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
        AddGridRow(table, "Payments made", Money(account.PaymentsMade), "CIS deducted", Money(account.CisDeducted));

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
