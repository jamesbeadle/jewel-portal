using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class CostCentreReconciliationRenderer
{
    private static void AddHeaderBand(Section section, CostCentreReconciliationDocument document)
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

        AddHeaderTitle(row.Cells[0], document);
        AddHeaderStamp(row.Cells[1], document);
        Hairline(section);
    }

    private static void AddHeaderTitle(Cell cell, CostCentreReconciliationDocument document)
    {
        DocumentBranding.AddLogo(cell, Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = cell.AddParagraph("COST CENTRE RECONCILIATION");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = cell.AddParagraph(string.IsNullOrWhiteSpace(document.ProjectReference)
            ? document.ProjectName
            : $"{document.ProjectReference} — {document.ProjectName}");
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;
    }

    private static void AddHeaderStamp(Cell cell, CostCentreReconciliationDocument document)
    {
        var stamp = cell.AddParagraph(document.Heading.ToUpperInvariant());
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = cell.AddParagraph($"Generated  {DateAndTime(document.GeneratedAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;
    }

    private static void AddDetailsGrid(Section section, CostCentreReconciliationDocument document)
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

        AddGridRow(table,
            "Project", document.ProjectName,
            "Client", document.ClientName);
        AddGridRow(table,
            "Cost centre", document.Heading,
            "Centre codes", string.Join(", ", document.CostCodes));
        AddGridRow(table,
            "Sales value", Money(document.SalesValue),
            "Target cost", Money(document.TargetCost));

        SpaceAfterTable(section);
    }
}
