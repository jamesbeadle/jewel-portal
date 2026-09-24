using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
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
            ("Supplier", 4.2, false), ("Scope", 8.0, false), ("Value", 2.4, true),
            ("Days on site", 1.6, true), ("Client nominated", 1.6, false));
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table,
                subcontractor.Supplier, subcontractor.Scope, ContractorsReportText.Money(subcontractor.Value),
                ContractorsReportText.Days(subcontractor.AttendanceDays),
                subcontractor.IsClientNominated ? ContractorsReportText.Yes : ContractorsReportText.Dash);
        SpaceAfterTable(section);
    }
}
