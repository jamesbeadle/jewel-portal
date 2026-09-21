namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>The bounds of a photo batch and of a stored image.</summary>
internal static class ProgressPhotoLimits
{
    /// <summary>Images accepted in one call — a week of site photographs, with room to spare.</summary>
    public const int MaxImagesPerBatch = 50;

    /// <summary>Longest edge of a stored image. Report 29's Word file ran to 4.2 MB on 80
    /// photographs at this size; phone originals are four times the pixels.</summary>
    public const int MaxEdgePixels = 1600;

    public const int JpegQuality = 85;

    /// <summary>A single image bigger than this is refused before it is opened.</summary>
    public const long MaxImageBytes = 40L * 1024 * 1024;

    /// <summary>The largest dimension the decoder is allowed to open — comfortably above any phone
    /// camera, far below what a small file declaring a huge canvas would decode to (a decompression
    /// bomb: a few MB of PNG unpacking to tens of GB). Refused by the library, not by us.</summary>
    public const ulong MaxDecodedEdgePixels = 12_000;

    /// <summary>Memory the decoder may hold for one image before it refuses.</summary>
    public const ulong MaxDecoderMemoryBytes = 1024UL * 1024 * 1024;
}
