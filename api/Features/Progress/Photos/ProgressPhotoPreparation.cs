using ImageMagick;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>
/// Turns an incoming image into the one that is stored. HEIC becomes JPEG (browsers other than
/// Safari cannot show HEIC, and the report generators cannot place it); every image is turned the
/// way its EXIF orientation says, shrunk so its longest edge is at most
/// <see cref="ProgressPhotoLimits.MaxEdgePixels"/>, and stripped of its metadata. PNG stays PNG.
/// A file the image library cannot open is refused with its own reason.
/// </summary>
internal static class ProgressPhotoPreparation
{
    private static readonly MagickGeometry ShrinkOnly = new($"{ProgressPhotoLimits.MaxEdgePixels}x{ProgressPhotoLimits.MaxEdgePixels}>");

    static ProgressPhotoPreparation()
    {
        ResourceLimits.Width = ProgressPhotoLimits.MaxDecodedEdgePixels;
        ResourceLimits.Height = ProgressPhotoLimits.MaxDecodedEdgePixels;
        ResourceLimits.Memory = ProgressPhotoLimits.MaxDecoderMemoryBytes;
    }

    public static PreparedProgressPhoto Prepare(IncomingProgressPhoto incoming)
    {
        if (incoming.Bytes.Length == 0)
            throw new InvalidDataException("The file is empty.");
        if (incoming.Bytes.Length > ProgressPhotoLimits.MaxImageBytes)
            throw new InvalidDataException($"The file is {incoming.Bytes.Length / 1_048_576.0:0.#} MB — larger than the {ProgressPhotoLimits.MaxImageBytes / 1_048_576} MB an image may be.");
        if (!ProgressPhotoFormats.IsAccepted(incoming.FileName, incoming.ContentType))
            throw new InvalidDataException($"Not a photograph the portal accepts ({ProgressPhotoFormats.Describe()}).");

        var contentHash = ProgressPhotoContentHash.Of(incoming.Bytes);
        var keepsPng = ProgressPhotoFormats.IsPng(incoming.FileName, incoming.ContentType);

        using var image = Open(incoming);
        image.AutoOrient();
        image.Resize(ShrinkOnly);
        image.Strip();
        image.Format = keepsPng ? MagickFormat.Png : MagickFormat.Jpeg;
        if (!keepsPng) image.Quality = ProgressPhotoLimits.JpegQuality;

        return new PreparedProgressPhoto(
            StoredFileName(incoming.FileName, keepsPng),
            keepsPng ? ProgressPhotoFormats.Png : ProgressPhotoFormats.Jpeg,
            image.ToByteArray(),
            contentHash);
    }

    private static MagickImage Open(IncomingProgressPhoto incoming)
    {
        try
        {
            return new MagickImage(incoming.Bytes);
        }
        catch (MagickException ex)
        {
            throw new InvalidDataException($"The image could not be opened ({ex.Message}).");
        }
    }

    /// <summary>The stored name keeps the original's stem; a converted HEIC is a .jpg from here on.</summary>
    private static string StoredFileName(string originalFileName, bool keepsPng)
    {
        var stem = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(stem)) stem = "photo";
        return stem + (keepsPng ? ".png" : ".jpg");
    }
}
