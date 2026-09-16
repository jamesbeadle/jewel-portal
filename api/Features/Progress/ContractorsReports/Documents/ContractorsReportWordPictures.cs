using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>Section 9's photographs in Word: two to a row in a borderless table, each scaled
/// to the column by its own pixel size so nothing is stretched.</summary>
internal sealed class ContractorsReportWordPictures
{
    private const long ColumnWidthEmu = 2_476_500;
    private const long EmuPerTwip = 635;
    private const string PictureDataUri = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private readonly OpenXmlPartContainer part;
    private uint nextPictureId;

    /// <param name="part">The part the pictures live in — the body, or a header — since an
    /// image belongs to the part that shows it.</param>
    public ContractorsReportWordPictures(OpenXmlPartContainer part, uint firstPictureId = 1)
    {
        this.part = part;
        nextPictureId = firstPictureId;
    }

    public Table PhotoGrid(IReadOnlyList<ContractorsReportImage> photos)
    {
        var table = new Table(new TableProperties
        {
            TableWidth = new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
            TableBorders = new TableBorders(
                new TopBorder { Val = BorderValues.None }, new LeftBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None }, new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None }, new InsideVerticalBorder { Val = BorderValues.None })
        });
        var columnTwips = (ColumnWidthEmu / EmuPerTwip).ToString();
        table.Append(new TableGrid(new GridColumn { Width = columnTwips }, new GridColumn { Width = columnTwips }));
        for (var index = 0; index < photos.Count; index += 2)
        {
            var row = new TableRow();
            row.Append(new TableCell(Picture(photos[index])));
            row.Append(new TableCell(index + 1 < photos.Count ? Picture(photos[index + 1]) : new Paragraph()));
            table.Append(row);
        }
        return table;
    }

    public Paragraph Picture(ContractorsReportImage image, long widthEmu = ColumnWidthEmu)
    {
        var imagePart = part.AddNewPart<ImagePart>(image.IsPng ? "image/png" : "image/jpeg");
        using (var stream = new MemoryStream(image.Content)) imagePart.FeedData(stream);
        var relationshipId = part.GetIdOfPart(imagePart);
        var heightEmu = (long)Math.Round(widthEmu * (double)image.PixelHeight / Math.Max(1, image.PixelWidth));
        var id = nextPictureId++;
        var name = $"Photo {id}";

        var inline = new DW.Inline(
            new DW.Extent { Cx = widthEmu, Cy = heightEmu },
            new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
            new DW.DocProperties { Id = id, Name = name },
            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(new A.GraphicData(Shape(relationshipId, name, widthEmu, heightEmu)) { Uri = PictureDataUri }))
        {
            DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U
        };
        return new Paragraph(new Run(new DocumentFormat.OpenXml.Wordprocessing.Drawing(inline)));
    }

    private static PIC.Picture Shape(string relationshipId, string name, long widthEmu, long heightEmu) =>
        new(
            new PIC.NonVisualPictureProperties(
                new PIC.NonVisualDrawingProperties { Id = 0U, Name = name },
                new PIC.NonVisualPictureDrawingProperties()),
            new PIC.BlipFill(
                new A.Blip { Embed = relationshipId },
                new A.Stretch(new A.FillRectangle())),
            new PIC.ShapeProperties(
                new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));
}
