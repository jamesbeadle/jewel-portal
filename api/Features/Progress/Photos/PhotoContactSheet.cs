using ImageMagick;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>
/// What the assistant is shown when it asks to see photographs (the FD's ask, 24 Sep 2026: the
/// week's sift and the report's choice happen inside the portal, never from laptop copies). One
/// photograph comes back as itself with its longest edge at most <see cref="SingleEdgePixels"/>;
/// several come back as ONE contact sheet — a tool result carries one image — laid out left to
/// right, top to bottom, in the order asked, so the caller's numbered legend says which is which.
/// No text is drawn on the sheet: a server without fonts must still build it.
/// </summary>
internal static class PhotoContactSheet
{
    public const int MostPhotos = 12;
    public const string MediaType = "image/jpeg";

    private const int SingleEdgePixels = 1000;
    private const int Columns = 4;
    private const int TilePixels = 384;
    private const int TileMarginPixels = 6;
    private const int JpegQuality = 80;

    public static byte[] Build(IReadOnlyList<byte[]> photos) =>
        photos.Count == 1 ? Single(photos[0]) : Sheet(photos);

    private static byte[] Single(byte[] photo)
    {
        using var image = Opened(photo);
        image.Resize(new MagickGeometry($"{SingleEdgePixels}x{SingleEdgePixels}>"));
        return AsJpeg(image);
    }

    private static byte[] Sheet(IReadOnlyList<byte[]> photos)
    {
        var rows = (photos.Count + Columns - 1) / Columns;
        var columns = Math.Min(Columns, photos.Count);
        using var sheet = new MagickImage(MagickColors.White, columns * TilePixels, rows * TilePixels);
        for (var index = 0; index < photos.Count; index++)
        {
            using var tile = Opened(photos[index]);
            var fit = TilePixels - 2 * TileMarginPixels;
            tile.Resize(new MagickGeometry(fit, fit));
            var left = index % Columns * TilePixels + (TilePixels - tile.Width) / 2;
            var top = index / Columns * TilePixels + (TilePixels - tile.Height) / 2;
            sheet.Composite(tile, left, top, CompositeOperator.Over);
        }
        return AsJpeg(sheet);
    }

    private static MagickImage Opened(byte[] photo)
    {
        var image = new MagickImage(photo);
        image.AutoOrient();
        return image;
    }

    private static byte[] AsJpeg(MagickImage image)
    {
        image.Strip();
        image.Format = MagickFormat.Jpeg;
        image.Quality = JpegQuality;
        return image.ToByteArray();
    }
}
