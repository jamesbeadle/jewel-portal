namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>Which files are photographs to the intake: JPEG, PNG and the HEIC/HEIF an iPhone
/// exports. Judged by extension first, then content type, because WhatsApp zips and some mail
/// clients send images as application/octet-stream.</summary>
internal static class ProgressPhotoFormats
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Heic = "image/heic";
    public const string Heif = "image/heif";

    private static readonly string[] JpegExtensions = { ".jpg", ".jpeg" };
    private static readonly string[] PngExtensions = { ".png" };
    private static readonly string[] HeicExtensions = { ".heic", ".heif" };

    public static bool IsAccepted(string fileName, string? contentType) =>
        IsJpeg(fileName, contentType) || IsPng(fileName, contentType) || IsHeic(fileName, contentType);

    public static bool IsJpeg(string fileName, string? contentType) =>
        HasExtension(fileName, JpegExtensions) || Is(contentType, Jpeg);

    public static bool IsPng(string fileName, string? contentType) =>
        HasExtension(fileName, PngExtensions) || Is(contentType, Png);

    public static bool IsHeic(string fileName, string? contentType) =>
        HasExtension(fileName, HeicExtensions) || Is(contentType, Heic) || Is(contentType, Heif);

    public static string Describe() => "JPEG, PNG or HEIC";

    private static bool HasExtension(string fileName, string[] extensions)
    {
        var extension = Path.GetExtension(fileName);
        return extensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static bool Is(string? contentType, string expected) =>
        string.Equals(contentType?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
