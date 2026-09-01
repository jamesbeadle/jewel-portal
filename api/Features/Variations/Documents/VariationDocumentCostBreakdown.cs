using MigraDoc.DocumentObjectModel;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// The document's cost breakdown: the priced build-up as it stands on the valuation report once
/// the variation is approved, or the quoting-stage estimate before then. Totals are net of VAT —
/// the valuation report the lines feed is an excl-VAT document, and the sheet says so.
/// </summary>
internal static class VariationDocumentCostBreakdown
{
    public static void Add(Section section, VariationDocumentModel model)
    {
        SectionHeading(section, "Cost breakdown");

        if (model.Lines.Count == 0)
        {
            AddPreApprovalSummary(section, model);
            SpaceAfterTable(section);
            return;
        }

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(2.6));   // Cost code
        table.AddColumn(Unit.FromCentimeter(8.0));   // Description
        table.AddColumn(Unit.FromCentimeter(1.6));   // Qty
        table.AddColumn(Unit.FromCentimeter(1.4));   // Unit
        table.AddColumn(Unit.FromCentimeter(2.1));   // Rate
        table.AddColumn(Unit.FromCentimeter(2.1));   // Amount

        var head = table.AddRow();
        head.Shading.Color = Navy;
        head.HeadingFormat = true;                   // repeat the header when the table breaks pages
        HeaderCell(head.Cells[0], "Cost code");
        HeaderCell(head.Cells[1], "Description");
        HeaderCell(head.Cells[2], "Qty");
        HeaderCell(head.Cells[3], "Unit");
        HeaderCell(head.Cells[4], "Rate £");
        HeaderCell(head.Cells[5], "Amount £");

        var zebra = false;
        foreach (var line in model.Lines)
        {
            var row = table.AddRow();
            if (zebra) row.Shading.Color = Panel;
            zebra = !zebra;
            BodyCell(row.Cells[0], line.CostCode);
            BodyCell(row.Cells[1], line.Description);
            BodyCell(row.Cells[2], line.Quantity.ToString("0.##", Uk));
            BodyCell(row.Cells[3], line.Unit);
            BodyCell(row.Cells[4], line.Rate.ToString("N2", Uk));
            BodyCell(row.Cells[5], line.Amount.ToString("N2", Uk));
        }

        var total = table.AddRow();
        total.Shading.Color = Panel;
        BodyCell(total.Cells[0], "");
        var totalLabel = total.Cells[1].AddParagraph("NET VO TOTAL (excl. VAT)");
        totalLabel.Format.Font.Size = 8.5;
        totalLabel.Format.Font.Bold = true;
        totalLabel.Format.Font.Color = Navy;
        total.Cells[1].Format.LeftIndent = Unit.FromMillimeter(1.5);
        total.Cells[1].MergeRight = 3;
        var totalValue = total.Cells[5].AddParagraph(model.LinesTotal.ToString("N2", Uk));
        totalValue.Format.Font.Size = 8.5;
        totalValue.Format.Font.Bold = true;
        totalValue.Format.Font.Color = model.LinesTotal < 0m ? Orange : Navy;
        total.Cells[5].Format.LeftIndent = Unit.FromMillimeter(1.5);

        SpaceAfterTable(section);
    }

    // Before approval nothing has been written to the valuation report, so the document carries
    // the estimate and says where the priced build-up will come from — never an empty table that
    // could read as "this variation is worth nothing".
    private static void AddPreApprovalSummary(Section section, VariationDocumentModel model)
    {
        var summary = model.EstimatedValue is { } estimate
            ? $"Estimated value {Money(estimate)} (excl. VAT). The priced line build-up is recorded on approval, when the value is written to the valuation report."
            : "Not yet priced. The priced line build-up is recorded on approval, when the value is written to the valuation report.";
        var paragraph = Panelled(section, summary);
        paragraph.Format.Font.Italic = true;
        paragraph.Format.Font.Color = Muted;
    }
}
