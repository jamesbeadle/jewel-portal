using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

using static Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents.ContractorsReportWordParts;
using static Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents.ContractorsReportWordTables;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

public static partial class ContractorsReportWordRenderer
{
    private static readonly RegisterColumn[] DecisionColumns =
        { new("Reference", 2.4), new("Title", 9.4), new("Status", 3.6), new("Response due", 2.4) };
    private static readonly RegisterColumn[] VariationColumns =
        { new("Variation", 11.4), new("Position", 6.4) };
    private static readonly RegisterColumn[] SubcontractorColumns = { new("Subcontractor", 5.4), new("Scope", 12.4) };

    private static void AddProgress(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.Progress));
        foreach (var day in model.Progress)
        {
            body.Append(DayHeading(day.Heading));
            if (day.Entries.Count == 0) body.Append(MutedLine(ContractorsReportText.NoProgressRecorded));
            foreach (var entry in day.Entries)
            {
                body.Append(Text(entry.Title, BodySize, true, false, Ink, 40));
                if (!string.IsNullOrWhiteSpace(entry.Description)) body.Append(Lines(entry.Description));
            }
        }
    }

    private static void AddLookAhead(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.LookAhead));
        var planned = ContractorsReportText.Planned(model);
        if (planned.Count == 0) { body.Append(MutedLine(ContractorsReportText.NoLookAhead)); return; }
        foreach (var item in planned) body.Append(Bullet(item.Text));
    }

    private static void AddDecisions(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.Decisions));
        if (model.Decisions.Count == 0) { body.Append(MutedLine(ContractorsReportText.NoDecisions)); return; }
        var table = Register(DecisionColumns);
        foreach (var decision in model.Decisions)
            BodyRow(table, DecisionColumns, decision.Reference, decision.Title, decision.Status, ContractorsReportText.Date(decision.ResponseDue));
        body.Append(table);
        body.Append(MutedLine(ContractorsReportText.DecisionsCount(model.Decisions.Count)));
    }

    private static void AddVariations(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.Variations));
        var nothingOutstanding = MutedLine(ContractorsReportVariationWording.NothingOutstanding(model));
        if (model.Variations.Count == 0) { body.Append(nothingOutstanding); return; }
        body.Append(Text(ContractorsReportVariationWording.Paragraph(model)));
        var table = Register(VariationColumns);
        foreach (var variation in model.Variations)
            BodyRow(table, VariationColumns, ContractorsReportVariationWording.Row(variation), ContractorsReportVariationWording.Position(variation));
        body.Append(table);
        body.Append(new Paragraph());
    }

    private static void AddBuildingControl(Body body, ContractorsReportBuildingControl buildingControl)
    {
        body.Append(SectionHeading(ContractorsReportSections.BuildingControl));
        body.Append(BuildingControlContact(buildingControl));
        body.Append(Panelled(ContractorsReportText.OrNothingToReport(buildingControl.Liaison)));
    }

    private static OpenXmlElement BuildingControlContact(ContractorsReportBuildingControl buildingControl)
    {
        if (buildingControl.HasCase())
            return Grid(
                ("Body", buildingControl.BodyName ?? ""), ("Contact", buildingControl.ContactName ?? ""),
                ("Email", buildingControl.ContactEmail ?? ""), ("Phone", buildingControl.ContactPhone ?? ""));
        if (buildingControl.EnteredContact is not { } contact) return MutedLine(ContractorsReportText.NoBuildingControlCase);
        var line = ContractorsReportText.BuildingControlContact(contact);
        return Text(line);
    }

    private static void AddSubcontractors(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.Subcontractors));
        if (model.Subcontractors.Count == 0) { body.Append(MutedLine(ContractorsReportText.NoSubcontractors)); return; }
        body.Append(Text(ContractorsReportText.SubcontractorsOpening));
        var table = Register(SubcontractorColumns);
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table, SubcontractorColumns, subcontractor.Supplier, subcontractor.Scope);
        body.Append(table);
        body.Append(new Paragraph());
    }

    private static void AddPhotographs(Body body, ContractorsReportWordPictures pictures, ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images)
    {
        body.Append(SectionHeading(ContractorsReportSections.Photographs));
        var days = ContractorsReportText.DaysWithPhotos(model);
        if (days.Count == 0) { body.Append(MutedLine(ContractorsReportText.NothingToReport)); return; }
        foreach (var day in days)
        {
            var loaded = day.Photos.Select(photo => images.GetValueOrDefault(photo.ProgressPhotoId)).OfType<ContractorsReportImage>().ToList();
            if (loaded.Count == 0) continue;
            body.Append(DayHeading(day.Heading));
            body.Append(pictures.PhotoGrid(loaded));
            body.Append(new Paragraph());
        }
        body.Append(MutedLine(ContractorsReportText.PhotographCount(days)));
    }
}
