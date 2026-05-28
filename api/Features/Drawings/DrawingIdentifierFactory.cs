namespace Jewel.JPMS.Api.Features.Drawings;

internal static class DrawingIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextDrawingId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextDrawingRevisionId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
