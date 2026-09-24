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
        var nothingOutstanding = ContractorsReportVariationWording.NothingOutstanding(model);
        if (model.Variations.Count == 0) { MutedLine(section, nothingOutstanding); return; }

        var opening = section.AddParagraph(ContractorsReportVariationWording.Paragraph(model));
        opening.Format.Font.Size = 9.5;
        SpaceAfter(opening, 2.5);
        var table = RegisterTable(section, ("Variation", 11.4, false), ("Position", 6.4, false));
        foreach (var variation in model.Variations)
            BodyRow(table, ContractorsReportVariationWording.Row(variation), ContractorsReportVariationWording.Position(variation));
        SpaceAfterTable(section);
    }

    private static void AddSubcontractors(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Subcontractors);
        if (model.Subcontractors.Count == 0) { MutedLine(section, ContractorsReportText.NoSubcontractors); return; }

        var opening = section.AddParagraph(ContractorsReportText.SubcontractorsOpening);
        opening.Format.Font.Size = 9.5;
        SpaceAfter(opening, 2.5);
        var table = RegisterTable(section, ("Subcontractor", 5.4, false), ("Scope", 12.4, false));
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table, subcontractor.Supplier, subcontractor.Scope);
        SpaceAfterTable(section);
    }
}
