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

        BodyLine(section, ContractorsReportPrintedText.DecisionsOpening);
        var dateOfIssue = model.Header.DateOfIssue;
        var table = RegisterTable(section, ("RFI", 11.4, false), ("Status", 6.4, false));
        foreach (var decision in model.Decisions)
            BodyRow(table, ContractorsReportPrintedText.DecisionRow(decision), ContractorsReportPrintedText.DecisionStatus(decision, dateOfIssue));
        SpaceAfterTable(section);
    }

    private static void AddVariations(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Variations);
        var nothingOutstanding = ContractorsReportVariationWording.NothingOutstanding(model);
        if (model.Variations.Count == 0) { MutedLine(section, nothingOutstanding); return; }

        BodyLine(section, ContractorsReportVariationWording.Paragraph(model));
        var table = RegisterTable(section, ("Variation", 11.4, false), ("Position", 6.4, false));
        foreach (var variation in model.Variations)
            BodyRow(table, ContractorsReportVariationWording.Row(variation), ContractorsReportVariationWording.Position(variation));
        SpaceAfterTable(section);
    }

    private static void AddSubcontractors(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Subcontractors);
        if (model.Subcontractors.Count == 0) { MutedLine(section, ContractorsReportText.NoSubcontractors); return; }

        BodyLine(section, ContractorsReportText.SubcontractorsOpening);
        var table = RegisterTable(section, ("Subcontractor", 4.6, false), ("Scope", 9.4, false), ("On site", 3.8, false));
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table, subcontractor.Supplier, subcontractor.Scope, ContractorsReportPrintedText.DaysOnSite(subcontractor));
        SpaceAfterTable(section);
    }
}
