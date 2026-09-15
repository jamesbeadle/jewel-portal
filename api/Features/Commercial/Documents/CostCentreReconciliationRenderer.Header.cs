using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class CostCentreReconciliationRenderer
{
    private static void AddHeaderBand(Section section, CostCentreReconciliationDocument document) =>
        CleanHeader(section, "Cost centre reconciliation",
            string.IsNullOrWhiteSpace(document.ProjectReference) ? document.ProjectName : $"{document.ProjectReference} — {document.ProjectName}",
            new HeaderFact(document.Heading.ToUpperInvariant()),
            new HeaderFact($"Generated  {DateAndTime(document.GeneratedAt)}"));

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
