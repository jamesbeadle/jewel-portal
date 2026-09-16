using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

public static partial class ContractorsReportPdfRenderer
{
    private static void AddProgress(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.Progress);
        foreach (var day in model.Progress)
        {
            DayHeading(section, day.Heading);
            if (day.Entries.Count == 0) MutedLine(section, ContractorsReportText.NoProgressRecorded);
            foreach (var entry in day.Entries) AddEntry(section, entry);
        }
    }

    private static void AddEntry(Section section, ContractorsReportEntry entry)
    {
        var title = section.AddParagraph(entry.Title);
        title.Format.Font.Size = 9.5;
        title.Format.Font.Bold = true;
        title.Format.KeepWithNext = true;
        SpaceAfter(title, 1);
        if (string.IsNullOrWhiteSpace(entry.Description)) return;
        var description = section.AddParagraph(entry.Description);
        description.Format.Font.Size = 9.5;
        SpaceAfter(description, 2.5);
    }

    private static void AddLookAhead(Section section, ContractorsReportDocument model)
    {
        SectionHeading(section, ContractorsReportSections.LookAhead);
        var planned = ContractorsReportText.Planned(model);
        if (planned.Count == 0) { MutedLine(section, ContractorsReportText.NoLookAhead); return; }
        foreach (var item in planned)
        {
            var line = section.AddParagraph("•  " + item.Text);
            line.Format.Font.Size = 9.5;
            line.Format.LeftIndent = Unit.FromMillimeter(4);
            line.Format.FirstLineIndent = Unit.FromMillimeter(-4);
            SpaceAfter(line, 1);
        }
    }

    private static void AddBuildingControl(Section section, ContractorsReportBuildingControl buildingControl)
    {
        SectionHeading(section, ContractorsReportSections.BuildingControl);
        if (buildingControl.BodyName is null && buildingControl.ContactName is null)
            MutedLine(section, ContractorsReportText.NoBuildingControlCase);
        else
            AddContactGrid(section, buildingControl);
        Panelled(section, ContractorsReportText.OrNothingToReport(buildingControl.Liaison));
        SpaceAfterTable(section);
    }

    private static void AddContactGrid(Section section, ContractorsReportBuildingControl buildingControl)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(3.3));
        table.AddColumn(Unit.FromCentimeter(5.6));
        table.AddColumn(Unit.FromCentimeter(3.3));
        table.AddColumn(Unit.FromCentimeter(5.6));
        AddGridRow(table, "Body", ContractorsReportText.OrDash(buildingControl.BodyName), "Contact", ContractorsReportText.OrDash(buildingControl.ContactName));
        AddGridRow(table, "Email", ContractorsReportText.OrDash(buildingControl.ContactEmail), "Phone", ContractorsReportText.OrDash(buildingControl.ContactPhone));
        SpaceAfterTable(section);
    }

    private static void AddPhotographs(Section section, ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images)
    {
        SectionHeading(section, ContractorsReportSections.Photographs);
        var days = ContractorsReportText.DaysWithPhotos(model);
        if (days.Count == 0) { MutedLine(section, ContractorsReportText.NothingToReport); return; }
        foreach (var day in days)
        {
            var loaded = day.Photos.Select(photo => images.GetValueOrDefault(photo.ProgressPhotoId)).OfType<ContractorsReportImage>().ToList();
            if (loaded.Count == 0) continue;
            DayHeading(section, day.Heading);
            AddPhotoGrid(section, loaded);
        }
    }

    private static void AddPhotoGrid(Section section, IReadOnlyList<ContractorsReportImage> photos)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));
        for (var index = 0; index < photos.Count; index += 2)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            AddPhotoCell(row.Cells[0], photos[index]);
            if (index + 1 < photos.Count) AddPhotoCell(row.Cells[1], photos[index + 1]);
        }
        SpaceAfterTable(section);
    }

    private static void AddPhotoCell(Cell cell, ContractorsReportImage photo)
    {
        var image = cell.AddImage("base64:" + Convert.ToBase64String(photo.Content));
        image.LockAspectRatio = true;
        image.Width = Unit.FromCentimeter(8.6);
    }

    private static void DayHeading(Section section, string heading)
    {
        var day = section.AddParagraph(heading);
        day.Format.Font.Size = 9;
        day.Format.Font.Bold = true;
        day.Format.Font.Color = Gold;
        day.Format.KeepWithNext = true;
        SpaceBefore(day, 3);
        SpaceAfter(day, 1.5);
    }
}
