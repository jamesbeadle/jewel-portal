using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

public static partial class ContractorsReportPdfRenderer
{
    private static void AddDecisions(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Decisions);
        if (model.Decisions.Count == 0) { MutedLine(section, ContractorsReportText.NoDecisions); return; }

        var table = RegisterTable(section, ("Reference", 2.4, false), ("Title", 9.4, false), ("Status", 3.6, false), ("Response due", 2.4, false));
        foreach (var decision in model.Decisions)
            BodyRow(table, decision.Reference, decision.Title, decision.Status, ContractorsReportText.Date(decision.ResponseDue));
        SpaceAfterTable(section);
        MutedLine(section, ContractorsReportText.DecisionsCount(model.Decisions.Count));
    }

    private static void AddVariations(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Variations);
        if (model.Variations.Count == 0) { MutedLine(section, ContractorsReportText.NoVariations); return; }

        var table = RegisterTable(section, ("No.", 1.8, false), ("Title", 10.0, false), ("Status", 3.0, false), ("Value (ex VAT)", 3.0, true));
        foreach (var variation in model.Variations)
            BodyRow(table, variation.DisplayNumber, variation.Title, variation.Status, ContractorsReportText.Money(variation.Value));
        var total = table.AddRow();
        total.Shading.Color = Panel;
        LabelCell(total.Cells[2], "Total");
        var totalCell = total.Cells[3];
        totalCell.Format.Alignment = ParagraphAlignment.Right;
        LabelCell(totalCell, ContractorsReportText.Money(model.VariationsTotal));
        SpaceAfterTable(section);
    }

    private static void AddSubcontractors(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Subcontractors);
        if (model.Subcontractors.Count == 0) { MutedLine(section, ContractorsReportText.NoSubcontractors); return; }

        var table = RegisterTable(section,
            ("Order", 1.8, false), ("Supplier", 3.6, false), ("Scope", 4.6, false), ("Value", 2.4, true),
            ("Target completion", 2.2, false), ("Days on site", 1.6, true), ("Client nominated", 1.6, false));
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table,
                subcontractor.Reference, subcontractor.Supplier, subcontractor.Scope,
                ContractorsReportText.Money(subcontractor.Value), ContractorsReportText.Date(subcontractor.TargetCompletion),
                ContractorsReportText.Days(subcontractor.AttendanceDays),
                subcontractor.IsClientNominated ? ContractorsReportText.Yes : ContractorsReportText.Dash);
        SpaceAfterTable(section);
    }

    private static Table RegisterTable(Section section, params (string Heading, double Centimetres, bool IsRightAligned)[] columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        foreach (var column in columns)
        {
            var added = table.AddColumn(Unit.FromCentimeter(column.Centimetres));
            if (column.IsRightAligned) added.Format.Alignment = ParagraphAlignment.Right;
        }
        var header = table.AddRow();
        header.Shading.Color = Navy;
        header.HeadingFormat = true;
        for (var index = 0; index < columns.Length; index++) HeaderCell(header.Cells[index], columns[index].Heading);
        return table;
    }

    private static void BodyRow(Table table, params string[] values)
    {
        var row = table.AddRow();
        for (var index = 0; index < values.Length; index++) BodyCell(row.Cells[index], values[index]);
    }
}
