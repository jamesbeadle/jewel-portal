namespace Jewel.JPMS.Api.Features.Progress;

internal static class ProgressIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextProgressUpdateId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgressPhotoId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgressReportId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProgressReportSelectionId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
