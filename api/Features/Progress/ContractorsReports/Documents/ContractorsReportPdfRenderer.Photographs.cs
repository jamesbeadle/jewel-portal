using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

public static partial class ContractorsReportPdfRenderer
{
    private static void AddPhotographs(Section section, ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images)
    {
        SectionHeading(section, ContractorsReportSections.Photographs);
        var days = ContractorsReportText.DaysWithPhotos(model);
        if (days.Count == 0) { MutedLine(section, ContractorsReportText.NothingToReport); return; }
        foreach (var day in days)
        {
            var loaded = day.Photos.Select(photo => images.GetValueOrDefault(photo.ProgressPhotoId)).OfType<ContractorsReportImage>().ToList();
            if (loaded.Count == 0) continue;
            DayHeading(section, ContractorsReportPrintedText.PhotoDayHeading(day));
            AddPhotoGrid(section, loaded);
        }
        MutedLine(section, ContractorsReportText.PhotographCount(days));
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

    /// <summary>Each photograph fits a box two across and three down, so a page carries six
    /// (Jeremy's review of Report 31: four a page ran the report to seventeen pages); a photograph
    /// of unknown size takes the box's width.</summary>
    private const double PhotoBoxWidthCm = 8.6;
    private const double PhotoBoxHeightCm = 6.4;

    private static void AddPhotoCell(Cell cell, ContractorsReportImage photo)
    {
        var image = cell.AddImage("base64:" + Convert.ToBase64String(photo.Content));
        image.LockAspectRatio = true;
        image.Width = Unit.FromCentimeter(PhotoWidthCm(photo));
    }

    private static double PhotoWidthCm(ContractorsReportImage photo)
    {
        if (photo.PixelWidth <= 0 || photo.PixelHeight <= 0) return PhotoBoxWidthCm;
        var aspect = (double)photo.PixelWidth / photo.PixelHeight;
        return Math.Min(PhotoBoxWidthCm, PhotoBoxHeightCm * aspect);
    }
}
