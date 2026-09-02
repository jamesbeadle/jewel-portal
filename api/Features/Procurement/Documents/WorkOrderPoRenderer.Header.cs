using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    private static void AddHeaderBand(Section section, WorkOrderPoDocumentModel model)
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

        AddHeaderTitle(row.Cells[0], model);
        AddCompanyBlock(row.Cells[1], model);
        Hairline(section);
    }

    private static void AddHeaderTitle(Cell cell, WorkOrderPoDocumentModel model)
    {
        DocumentBranding.AddLogo(cell, Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = cell.AddParagraph("PURCHASE ORDER");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = cell.AddParagraph(TitleOrSupplier(model));
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;
    }

    // The sheet's top-right identity, letter-style: the reference, then the company block.
    private static void AddCompanyBlock(Cell cell, WorkOrderPoDocumentModel model)
    {
        var stamp = cell.AddParagraph(model.Order.Reference);
        stamp.Format.Font.Size = 13;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        foreach (var line in CompanyAddress)
        {
            var p = cell.AddParagraph(line);
            p.Format.Font.Size = 7.5;
            p.Format.Font.Color = Gold;
        }
    }

    private static readonly string[] CompanyAddress =
    {
        "Jewel Bespoke Build Ltd",
        "Argent House, 175 Hook Rise South,",
        "Surbiton, Greater London, KT6 7LD",
        "Phone: 0208 109 1014"
    };

    private static string TitleOrSupplier(WorkOrderPoDocumentModel model) =>
        string.IsNullOrWhiteSpace(model.Order.Title) ? model.SupplierName : model.Order.Title;

    private static void AddDetailsGrid(Section section, WorkOrderPoDocumentModel model)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(1.5);
        spacer.Format.Font.Size = 2;

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelW = Unit.FromCentimeter(3.3);
        var valueW = Unit.FromCentimeter(5.6);
        table.AddColumn(labelW);
        table.AddColumn(valueW);
        table.AddColumn(labelW);
        table.AddColumn(valueW);

        // "— on approval": a draft has not been released, so AwardedAt still holds the drafting
        // stamp — printing it would date a release that hasn't happened (same rule as the sheet).
        AddGridRow(table,
            "Purchase order #", model.Order.Reference,
            "Total price", Money(model.Order.Value));
        AddGridRow(table,
            "Date created", Date(model.Order.CreatedAt),
            "Date released", model.Order.IsDraft ? "— on approval" : Date(model.Order.AwardedAt));
        AddGridRow(table,
            "Scheduled completion", CompletionOrDash(model),
            "Payment terms", $"{model.PaymentTermsDays} days");
        SpaceAfterTable(section);
    }

    private static string CompletionOrDash(WorkOrderPoDocumentModel model) =>
        model.Order.ScheduledCompletion is { } completion ? Date(completion) : "—";

    private static void AddParties(Section section, WorkOrderPoDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.5);
        row.BottomPadding = Unit.FromMillimeter(1);

        PartyBlock(row.Cells[0], "SUB / VENDOR", model.SupplierName,
            new[] { model.SupplierContactName }.Concat(model.SupplierAddressLines));
        PartyBlock(row.Cells[1], "JOB", model.ProjectName, model.SiteAddressLines);
        SpaceAfterTable(section);
    }

    private static void PartyBlock(Cell cell, string label, string name, IEnumerable<string> lines)
    {
        var heading = cell.AddParagraph(label);
        heading.Format.Font.Size = 7.5;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Orange;
        SpaceAfter(heading, 1);

        var strong = cell.AddParagraph(string.IsNullOrWhiteSpace(name) ? "—" : name);
        strong.Format.Font.Size = 9.5;
        strong.Format.Font.Bold = true;
        strong.Format.Font.Color = Navy;

        foreach (var line in lines.Where(part => !string.IsNullOrWhiteSpace(part)))
        {
            var p = cell.AddParagraph(line);
            p.Format.Font.Size = 8.5;
        }
    }
}
