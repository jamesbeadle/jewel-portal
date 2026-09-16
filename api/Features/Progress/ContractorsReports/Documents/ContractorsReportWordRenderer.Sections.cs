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
        { new("No.", 1.8), new("Title", 10.0), new("Status", 3.0), new("Value (ex VAT)", 3.0, true) };
    private static readonly RegisterColumn[] SubcontractorColumns =
    {
        new("Order", 1.8), new("Supplier", 3.6), new("Scope", 4.6), new("Value", 2.4, true),
        new("Target completion", 2.2), new("Days on site", 1.6, true), new("Client nominated", 1.6)
    };

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
        if (model.Variations.Count == 0) { body.Append(MutedLine(ContractorsReportText.NoVariations)); return; }
        var table = Register(VariationColumns);
        foreach (var variation in model.Variations)
            BodyRow(table, VariationColumns, variation.DisplayNumber, variation.Title, variation.Status, ContractorsReportText.Money(variation.Value));
        TotalRow(table, VariationColumns, 2, "Total", ContractorsReportText.Money(model.VariationsTotal));
        body.Append(table);
        body.Append(new Paragraph());
    }

    private static void AddBuildingControl(Body body, ContractorsReportBuildingControl buildingControl)
    {
        body.Append(SectionHeading(ContractorsReportSections.BuildingControl));
        if (buildingControl.BodyName is null && buildingControl.ContactName is null)
            body.Append(MutedLine(ContractorsReportText.NoBuildingControlCase));
        else
            body.Append(Grid(
                ("Body", buildingControl.BodyName ?? ""), ("Contact", buildingControl.ContactName ?? ""),
                ("Email", buildingControl.ContactEmail ?? ""), ("Phone", buildingControl.ContactPhone ?? "")));
        body.Append(Panelled(ContractorsReportText.OrNothingToReport(buildingControl.Liaison)));
    }

    private static void AddSubcontractors(Body body, ContractorsReportDocument model)
    {
        body.Append(SectionHeading(ContractorsReportSections.Subcontractors));
        if (model.Subcontractors.Count == 0) { body.Append(MutedLine(ContractorsReportText.NoSubcontractors)); return; }
        var table = Register(SubcontractorColumns);
        foreach (var subcontractor in model.Subcontractors)
            BodyRow(table, SubcontractorColumns,
                subcontractor.Reference, subcontractor.Supplier, subcontractor.Scope,
                ContractorsReportText.Money(subcontractor.Value), ContractorsReportText.Date(subcontractor.TargetCompletion),
                ContractorsReportText.Days(subcontractor.AttendanceDays),
                subcontractor.IsClientNominated ? ContractorsReportText.Yes : ContractorsReportText.Dash);
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
    }
}
