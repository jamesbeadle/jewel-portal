namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>A photograph as the renderers take it: the bytes, what they are, and the pixel
/// size — Word needs the size to place an image at a set width without stretching it.</summary>
public sealed record ContractorsReportImage(string ProgressPhotoId, byte[] Content, string ContentType, int PixelWidth, int PixelHeight)
{
    public bool IsPng => ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Reads a JPEG's or PNG's pixel size from its header — the two formats every stored
/// photograph is in after intake (HEIC becomes JPEG on the way in).</summary>
internal static class ImagePixelSize
{
    private const int PngHeaderLength = 24;
    private const byte JpegMarkerPrefix = 0xFF;

    public static (int Width, int Height)? Of(byte[] bytes)
    {
        if (IsPng(bytes)) return PngSize(bytes);
        if (IsJpeg(bytes)) return JpegSize(bytes);
        return null;
    }

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= PngHeaderLength && bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G';

    private static (int, int) PngSize(byte[] bytes) =>
        (BigEndian(bytes, 16), BigEndian(bytes, 20));

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length > 4 && bytes[0] == JpegMarkerPrefix && bytes[1] == 0xD8;

    private static (int, int)? JpegSize(byte[] bytes)
    {
        var offset = 2;
        while (offset + 9 < bytes.Length)
        {
            if (bytes[offset] != JpegMarkerPrefix) { offset++; continue; }
            var marker = bytes[offset + 1];
            if (marker == JpegMarkerPrefix) { offset++; continue; }
            var length = (bytes[offset + 2] << 8) | bytes[offset + 3];
            if (IsStartOfFrame(marker))
                return (((bytes[offset + 7] << 8) | bytes[offset + 8]), ((bytes[offset + 5] << 8) | bytes[offset + 6]));
            offset += 2 + length;
        }
        return null;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;

    private static int BigEndian(byte[] bytes, int at) =>
        (bytes[at] << 24) | (bytes[at + 1] << 16) | (bytes[at + 2] << 8) | bytes[at + 3];
}
